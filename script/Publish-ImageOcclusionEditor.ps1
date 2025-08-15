<#
.SYNOPSIS
    Publish ImageOcclusionEditorWinUI3 to an organized "out" folder structure.

.DESCRIPTION
    Runs "dotnet publish" to build and publish the WinUI3 desktop app.
    Output folder structure example:
        out/ImageOcclusionEditor/<Configuration>/<TargetFramework>/<RuntimeIdentifier>/

.PARAMETER Configuration
    Build configuration. Default: Release.

.PARAMETER OutputRoot
    Root output folder. Default: repository-root/out.

.NOTES
    Target Framework (TFM) and Runtime Identifier (RID) are read from the project file.

.EXAMPLE
    ./script/Publish-ImageOcclusionEditor.ps1 -Configuration Release

.NOTES
    Requires .NET SDK and necessary workloads.
#>
[CmdletBinding(PositionalBinding=$false)]
param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release',
    [string]$OutputRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'
$PSNativeCommandUseErrorActionPreference = $true
$PSStyle.OutputRendering = 'Ansi'

function Resolve-RepoRoot {
    # script/ is a subfolder of repo root
    $scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Path $MyInvocation.MyCommand.Path -Parent }
    return (Resolve-Path -LiteralPath (Join-Path $scriptDir '..')).Path
}

function Get-ProjectFramework {
    param([string]$CsprojPath)
    if (-not (Test-Path -LiteralPath $CsprojPath)) {
        throw "Project file not found: $CsprojPath"
    }
    try {
    [xml]$xml = Get-Content -LiteralPath $CsprojPath -Raw -ErrorAction Stop
    $node = $xml.SelectSingleNode('/Project/PropertyGroup/TargetFramework')
    $tfm = if ($null -ne $node) { $node.InnerText } else { $null }
    if ([string]::IsNullOrWhiteSpace($tfm)) {
            throw 'Could not find <TargetFramework> in csproj.'
        }
        return $tfm
    }
    catch {
        throw "Failed to read TargetFramework: $($_.Exception.Message)"
    }
}

function Get-ProjectRuntimeIdentifier {
    param([string]$CsprojPath)
    if (-not (Test-Path -LiteralPath $CsprojPath)) {
        throw "Project file not found: $CsprojPath"
    }
    try {
        [xml]$xml = Get-Content -LiteralPath $CsprojPath -Raw -ErrorAction Stop
        $node = $xml.SelectSingleNode('/Project/PropertyGroup/RuntimeIdentifier')
        $rid = if ($null -ne $node) { $node.InnerText } else { $null }
        if ([string]::IsNullOrWhiteSpace($rid)) {
            $node = $xml.SelectSingleNode('/Project/PropertyGroup/RuntimeIdentifiers')
            if ($null -ne $node) {
                $list = @($node.InnerText -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
                if ($list.Length -gt 0) { $rid = $list[0] }
            }
        }
        return $rid
    }
    catch {
        throw "Failed to read RuntimeIdentifier(s): $($_.Exception.Message)"
    }
}

function New-OutputPath {
    [CmdletBinding(SupportsShouldProcess=$true)]
    param(
        [string]$Root,
        [string]$AssemblyName,
        [string]$Config,
        [string]$Tfm,
        [string]$Rid
    )
    $path = Join-Path $Root $AssemblyName
    $path = Join-Path $path $Config
    $path = Join-Path $path $Tfm
    $path = Join-Path $path $Rid
    if ($PSCmdlet.ShouldProcess($path, 'Create output directory')) {
        New-Item -ItemType Directory -Path $path -Force | Out-Null
    }
    return $path
}

# 1) Resolve paths and inputs
$repoRoot = Resolve-RepoRoot
$projectDir = Join-Path $repoRoot 'ImageOcclusionEditorWinUI3'
$csprojPath = Join-Path $projectDir 'ImageOcclusionEditorWinUI3.csproj'
$assemblyName = 'ImageOcclusionEditor'  # Must match <AssemblyName> in csproj
${Framework} = Get-ProjectFramework -CsprojPath $csprojPath
${Runtime} = Get-ProjectRuntimeIdentifier -CsprojPath $csprojPath

if (-not $OutputRoot) {
    $OutputRoot = Join-Path $repoRoot 'out'
}
$publishDir = New-OutputPath -Root $OutputRoot -AssemblyName $assemblyName -Config $Configuration -Tfm $Framework -Rid $Runtime

Write-Information "Publish info:"
Write-Information "  Project:       $csprojPath"
Write-Information "  Configuration: $Configuration"
Write-Information "  Framework:     $Framework"
Write-Information "  Runtime:       $Runtime"
Write-Information "  Output:        $publishDir"

# 2) Clean target output directory
if (Test-Path -LiteralPath $publishDir) {
    Write-Information "Cleaning output directory..."
    Remove-Item -LiteralPath $publishDir -Recurse -Force -ErrorAction Stop
}
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

# 3) Compose dotnet publish arguments
$publishArgs = @(
    'publish', $csprojPath,
    '-c', $Configuration,
    '-f', $Framework,
    '-o', $publishDir,
    '--nologo'
)

if ($Runtime) {
    $publishArgs += @('-r', $Runtime)
}

# Use locked restore mode if lock file exists
$lockFile = Join-Path $projectDir 'packages.lock.json'
if (Test-Path -LiteralPath $lockFile) {
    $publishArgs += @('/p:RestoreLockedMode=true')
}

# 4) Run publish
$cmdLine = 'dotnet ' + ($publishArgs -join ' ')
Write-Verbose "Run: $cmdLine"
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet @publishArgs
$exit = $LASTEXITCODE
$stopwatch.Stop()
if ($exit -ne 0) {
    throw "dotnet publish failed, exit code: $exit"
}
Write-Information ("Publish done in {0}s" -f [Math]::Round($stopwatch.Elapsed.TotalSeconds,2))

# 5) Show key outputs (AOT vs non-AOT layouts may differ)
$exe = Join-Path $publishDir ($assemblyName + '.exe')
$runtimeConfig = Join-Path $publishDir ($assemblyName + '.runtimeconfig.json')
$deps = Join-Path $publishDir ($assemblyName + '.deps.json')

Write-Information "Output file check:"
foreach ($f in @($exe, $runtimeConfig, $deps)) {
    $exists = Test-Path -LiteralPath $f
    $mark = if ($exists) { '[x]' } else { '[ ]' }
    Write-Information ("  {0} {1}" -f $mark, $f)
}
Write-Information "It's normal to see missing runtimeconfig.json and deps.json files for AOT builds."
