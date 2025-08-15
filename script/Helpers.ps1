<#
.SYNOPSIS
  Shared helper functions for ImageOcclusionEditor build and packaging scripts.

.NOTES
  This file is intended to be dot-sourced by other scripts in the same folder:
    . "$PSScriptRoot/Build-Helpers.ps1"
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -ge 7) {
  $global:PSNativeCommandUseErrorActionPreference = $true
}

function Write-Status {
  param(
    [Parameter(Mandatory)][string]$Message,
    [ValidateSet('Info','Warn','Error','Success')]
    [string]$Level = 'Info'
  )
  switch ($Level) {
    'Info'    { Write-Information "[>] $Message" -InformationAction Continue }
    'Warn'    { Write-Warning     "[!] $Message" }
    'Error'   { Write-Error       "[x] $Message" }
    'Success' { Write-Information "[OK] $Message" -InformationAction Continue }
  }
}

function Get-RepoRoot {
  <# Returns repository root directory assuming this file is under repoRoot/script #>
  return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
}

function Get-ProjectInfo {
  <#
  .SYNOPSIS
    Reads basic project info from a csproj: TargetFramework, AssemblyName, RuntimeIdentifier(s).
  .PARAMETER CsprojPath
    Path to the csproj file.
  .PARAMETER DefaultAssemblyName
    Fallback if AssemblyName is not present. Default: ImageOcclusionEditor
  .PARAMETER DefaultRid
    Optional fallback RID if not declared in project. If not provided, returns $null when not found.
  .OUTPUTS
    PSCustomObject @{ TargetFramework; AssemblyName; RuntimeIdentifier }
  #>
  param(
    [Parameter(Mandatory)][string]$CsprojPath,
    [string]$DefaultAssemblyName = 'ImageOcclusionEditor',
    [string]$DefaultRid
  )
  if (-not (Test-Path -LiteralPath $CsprojPath -PathType Leaf)) {
    throw "Project file not found: $CsprojPath"
  }
  [xml]$projXml = Get-Content -LiteralPath $CsprojPath -Raw
  $tfmNode = $projXml.SelectSingleNode('/Project/PropertyGroup/TargetFramework')
  $tfm = if ($tfmNode) { $tfmNode.InnerText } else { $null }
  if ([string]::IsNullOrWhiteSpace($tfm)) { throw 'TargetFramework not found in csproj.' }

  $asmNode = $projXml.SelectSingleNode('/Project/PropertyGroup/AssemblyName')
  $asm = if ($asmNode) { $asmNode.InnerText } else { $null }
  if ([string]::IsNullOrWhiteSpace($asm)) { $asm = $DefaultAssemblyName }

  $ridNode = $projXml.SelectSingleNode('/Project/PropertyGroup/RuntimeIdentifier')
  $rid = if ($ridNode) { $ridNode.InnerText } else { $null }
  if ([string]::IsNullOrWhiteSpace($rid)) {
    $ridsNode = $projXml.SelectSingleNode('/Project/PropertyGroup/RuntimeIdentifiers')
    if ($ridsNode) {
      $rids = @($ridsNode.InnerText -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
      if ($rids.Count -gt 0) { $rid = $rids[0] }
    }
  }
  if ([string]::IsNullOrWhiteSpace($rid) -and $DefaultRid) { $rid = $DefaultRid }

  [pscustomobject]@{
    TargetFramework    = $tfm
    AssemblyName       = $asm
    RuntimeIdentifier  = $rid
  }
}

function Get-PublishOutputPath {
  <#
  .SYNOPSIS
    Builds the publish output path layout used by Publish-ImageOcclusionEditor.ps1
  .DESCRIPTION
    out/ImageOcclusionEditor/<Configuration>/<TFM>/<RID>/
  #>
  param(
    [Parameter(Mandatory)][string]$PublishOutputRoot,
    [Parameter(Mandatory)][string]$Configuration,
    [Parameter(Mandatory)][string]$TargetFramework,
    [Parameter(Mandatory)][string]$RuntimeIdentifier
  )
  $root = Join-Path $PublishOutputRoot 'ImageOcclusionEditor'
  $path = Join-Path $root $Configuration
  $path = Join-Path $path $TargetFramework
  $path = Join-Path $path $RuntimeIdentifier
  return $path
}

function Get-ISCCPath {
  <# Locates Inno Setup compiler (ISCC.exe) from hint, PATH or common install paths. #>
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
