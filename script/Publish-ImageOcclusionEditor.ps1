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

# Dot-source shared helpers
. (Join-Path $PSScriptRoot 'Helpers.ps1')

# 1) Resolve paths and inputs
$repoRoot = Get-RepoRoot
$projectDir = Join-Path $repoRoot 'ImageOcclusionEditorWinUI3'
$csprojPath = Join-Path $projectDir 'ImageOcclusionEditorWinUI3.csproj'
$proj = Get-ProjectInfo -CsprojPath $csprojPath -DefaultAssemblyName 'ImageOcclusionEditor'
$assemblyName = $proj.AssemblyName
${Framework} = $proj.TargetFramework
${Runtime} = $proj.RuntimeIdentifier

if (-not $OutputRoot) {
    $OutputRoot = Join-Path $repoRoot 'out'
}
$publishDir = Get-PublishOutputPath -PublishOutputRoot $OutputRoot -Configuration $Configuration -TargetFramework $Framework -RuntimeIdentifier $Runtime

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
