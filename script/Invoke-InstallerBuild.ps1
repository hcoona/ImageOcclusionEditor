<#
.SYNOPSIS
Invokes publishing the WinUI3 app and builds the installer via Inno Setup.

.DESCRIPTION
This script publishes the WinUI3 app and then runs the Inno Setup compiler (ISCC) using the `Setup.iss` located in the same directory as this script (`script/Setup.iss`).
Steps:
- Publish the WinUI3 app (self-contained, RID-specific)
- Run Inno Setup compiler (ISCC) on `script/Setup.iss`

It follows PowerShell best practices and treats non-zero exit codes from native commands as terminating errors using PSNative preferences.

.PARAMETER Configuration
Build configuration. Defaults to Release. Accepted: Release, Debug

.PARAMETER Platform
CPU platform. Defaults to x64. Currently only x64 is supported by the project.

.PARAMETER RuntimeIdentifier
Runtime Identifier (RID) for publish. Defaults to win-x64.

.PARAMETER InstallerOutputPath
Output folder for the built installer. Defaults to repository root `out` directory.

.PARAMETER InnoSetupCompiler
Path to Inno Setup compiler (ISCC.exe). If not provided, the script searches PATH and common install locations.

.PARAMETER SkipPublish
Skip the publish step and only build the installer from existing published output.

.PARAMETER Clean
Remove publish and installer output directories before building.

.PARAMETER CleanOnly
Only clean the output directories and exit without building.

.EXAMPLE
# From repository root or any location
pwsh -File .\script\Invoke-InstallerBuild.ps1 -Configuration Release

.EXAMPLE
# Only build installer using existing publish output
pwsh -File .\script\Invoke-InstallerBuild.ps1 -SkipPublish

.NOTES
- Requires .NET SDK and Inno Setup 6.
- The publish output path is derived from the WinUI3 csproj's TargetFramework and RID.
#>

[CmdletBinding(PositionalBinding = $false, SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [ValidateSet('Release','Debug')]
    [string]$Configuration = 'Release',

    [ValidateSet('x64')]
    [string]$Platform = 'x64',

    [ValidateNotNullOrEmpty()]
    [string]$RuntimeIdentifier = 'win-x64',

    [string]$InstallerOutputPath,

    [string]$InnoSetupCompiler,

    [switch]$SkipPublish,

    [switch]$Clean,

    [switch]$CleanOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -ge 7) {
    # Promote non-zero exit codes from native commands to terminating errors
    $global:PSNativeCommandUseErrorActionPreference = $true
}

# Parameter validation: -Clean and -SkipPublish are mutually exclusive
if ($Clean -and $SkipPublish) {
    throw 'Parameters -Clean and -SkipPublish cannot be used together.'
}

# Utility: Write compact status
function Write-Status {
    param(
        [Parameter(Mandatory)][string]$Message,
        [ValidateSet('Info','Warn','Error','Success')]
        [string]$Level = 'Info'
    )
    # Use Write-Information/Warning/Error for host-agnostic output. Avoid non-ASCII symbols for encoding portability.
    switch ($Level) {
        'Info'    { Write-Information "[>] $Message" -InformationAction Continue }
        'Warn'    { Write-Warning     "[!] $Message" }
        'Error'   { Write-Error       "[x] $Message" }
        'Success' { Write-Information "[OK] $Message" -InformationAction Continue }
    }
}

# Resolve repo paths relative to this script
$ScriptDir = $PSScriptRoot
$RepoRoot  = Split-Path -Parent $ScriptDir
# WinUI3 csproj sits in project folder at repo root
$WinUIProj = Join-Path $RepoRoot 'ImageOcclusionEditorWinUI3/ImageOcclusionEditorWinUI3.csproj'
# Use Setup.iss in the same directory as this script
$SetupDir  = $ScriptDir
$SetupIss  = Join-Path $SetupDir  'Setup.iss'

if (-not (Test-Path -Path $WinUIProj -PathType Leaf)) {
    throw "WinUI3 project not found: $WinUIProj"
}
if (-not (Test-Path -Path $SetupIss -PathType Leaf)) {
    throw "Inno Setup script not found: $SetupIss"
}

# Read TargetFramework (TFM) and AssemblyName from the csproj to form publish paths (robust via XPath)
[xml]$projXml = Get-Content -LiteralPath $WinUIProj -Raw
$tfmNode = Select-Xml -Xml $projXml -XPath '//Project/PropertyGroup/TargetFramework' | Select-Object -First 1
if ($tfmNode) {
    $TargetFramework = $tfmNode.Node.InnerText
}
if ([string]::IsNullOrWhiteSpace($TargetFramework)) { throw 'TargetFramework not found in WinUI3 csproj.' }

$asmNode = Select-Xml -Xml $projXml -XPath '//Project/PropertyGroup/AssemblyName' | Select-Object -First 1
$AssemblyName = if ($asmNode) { $asmNode.Node.InnerText } else { 'ImageOcclusionEditor' }

# Compute default paths if not supplied
# The publish output still goes under the WinUI3 project folder
$PublishOutputPath = Join-Path (Split-Path -Parent $WinUIProj) (Join-Path "bin/$Platform/$Configuration" (Join-Path $TargetFramework (Join-Path $RuntimeIdentifier 'publish')))
if (-not $InstallerOutputPath) {
    # Default installer output to repo root 'out' directory
    $InstallerOutputPath = Join-Path $RepoRoot 'out'
}
$InstallerOutputPath = (Resolve-Path -LiteralPath (New-Item -ItemType Directory -Force -Path $InstallerOutputPath)).Path

Write-Status "Configuration: $Configuration | Platform: $Platform | RID: $RuntimeIdentifier" 'Info'
Write-Status "TFM: $TargetFramework | App Name: $AssemblyName" 'Info'
Write-Status "Publish Output: $PublishOutputPath" 'Info'
Write-Status "Installer Output: $InstallerOutputPath" 'Info'

# Clean outputs if requested
if ($Clean) {
    foreach ($p in @($PublishOutputPath, $InstallerOutputPath)) {
        if (Test-Path -LiteralPath $p) {
            if ($PSCmdlet.ShouldProcess($p, 'Remove Directory')) {
                Write-Status "Removing $p" 'Warn'
                Remove-Item -LiteralPath $p -Recurse -Force -ErrorAction Stop
            }
        }
    }
    if ($CleanOnly) {
        Write-Status 'Clean complete. Exiting due to -CleanOnly.' 'Success'
        return
    }
}

# Helper to locate ISCC.exe if not provided
function Get-ISCCPath {
    param([string]$Hint)
    if ($Hint) {
        if (Test-Path -LiteralPath $Hint) { return (Resolve-Path -LiteralPath $Hint).Path }
        throw "Inno Setup compiler not found at: $Hint"
    }
    $cmd = Get-Command -Name 'iscc','iscc.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($cmd) { return $cmd.Source }
    $candidates = @(
        "$($env:LOCALAPPDATA)\Programs\Inno Setup 6\ISCC.exe",
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 6\ISCC.exe'
    )
    foreach ($c in $candidates) { if (Test-Path -LiteralPath $c) { return $c } }
    throw 'Inno Setup compiler (ISCC.exe) not found. Install Inno Setup 6 or pass -InnoSetupCompiler.'
}

$ISCC = Get-ISCCPath -Hint $InnoSetupCompiler
Write-Status "Using ISCC: $ISCC" 'Info'

# Publish step
if (-not $SkipPublish) {
    Write-Status 'Publishing WinUI3 app...' 'Info'
    $publishArgs = @(
        'publish',
        $WinUIProj,
        '-c', $Configuration,
        '-r', $RuntimeIdentifier,
        '-o', $PublishOutputPath,
        "-p:Platform=$Platform",
        '--nologo',
        '-v', 'minimal'
    )
    # dotnet publish
    & dotnet @publishArgs
    # Validate expected output
    $exePath = Join-Path $PublishOutputPath ("{0}.exe" -f $AssemblyName)
    if (-not (Test-Path -LiteralPath $exePath)) {
        throw "Publish seems to have failed. Expected executable not found: $exePath"
    }
    Write-Status "Publish complete: $exePath" 'Success'
} else {
    Write-Status 'Skipping publish as requested (-SkipPublish).' 'Warn'
}

# Build installer
Write-Status 'Building installer with Inno Setup...' 'Info'
# Ensure output directory exists
if (-not (Test-Path -LiteralPath $InstallerOutputPath)) { New-Item -ItemType Directory -Force -Path $InstallerOutputPath | Out-Null }

# Invoke ISCC. /O specifies output folder. Always pass PublishDir to align with actual publish output.
# Do not embed quotes; ensure no trailing backslash for PublishDir.
$outArg = '/O' + $InstallerOutputPath
if ($PublishOutputPath.EndsWith('\')) { $PublishOutputPath = $PublishOutputPath.TrimEnd('\') }
$definePublishDir = '/DPublishDir=' + $PublishOutputPath
& $ISCC $SetupIss $outArg $definePublishDir

# Try to discover the output installer file (by convention from .csproj)
$expectedInstaller = Join-Path $InstallerOutputPath 'ImageOcclusionEditorWinUI3_Setup.exe'
if (Test-Path -LiteralPath $expectedInstaller) {
    Write-Status "Installer built: $expectedInstaller" 'Success'
} else {
    # Fallback: list most recent .exe in output folder
    $latest = Get-ChildItem -LiteralPath $InstallerOutputPath -Filter '*.exe' -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latest) {
        Write-Status "Installer built (detected): $($latest.FullName)" 'Success'
    } else {
        Write-Status 'ISCC finished but no installer .exe was found in the output folder.' 'Error'
        throw 'Installer output not found.'
    }
}
