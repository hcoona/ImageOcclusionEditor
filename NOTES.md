# License Management

Because we release our software as a packaged installer, we need to ensure that all dependencies are properly licensed and documented. This includes both direct dependencies and transitive dependencies.

## Generate CycloneDX SBOM for ImageOcclusionEditorWinUI3

```powershell
dotnet-CycloneDX ImageOcclusionEditorWinUI3\ImageOcclusionEditorWinUI3.csproj -o manifest --exclude-dev --exclude-test-projects --output-format Json
```

## List Licenses

```powershell
jq '.components | group_by(.licenses[0].license.id // "Unknown") | .[] | {license: (.[0].licenses[0].license.id // "Unknown"), packages: [.[] | "\(.name)@\(.version)"]}' "manifest\bom.json"
```

## Sample Output

```json
{
  "license": "GPL-3.0-or-later",
  "packages": [
    "IO.Github.Hcoona.Pngcs@1.2.1"
  ]
}
{
  "license": "MIT",
  "packages": [
    "ExCSS@4.3.1",
    "FileSignatures@5.3.0",
    "HarfBuzzSharp@8.3.0.1",
    "HarfBuzzSharp.NativeAssets.Win32@8.3.0.1",
    "Microsoft.Build.Tasks.Git@8.0.0",
    "Microsoft.SourceLink.Common@8.0.0",
    "runtime.win-x64.Microsoft.DotNet.ILCompiler@9.0.7",
    "ShimSkiaSharp@3.0.4",
    "SkiaSharp@3.116.1",
    "SkiaSharp.HarfBuzz@3.116.1",
    "SkiaSharp.NativeAssets.Win32@3.116.1",
    "Svg.Model@3.0.4",
    "Svg.Skia@3.0.4",
    "System.Text.Json@9.0.7"
  ]
}
{
  "license": "MPL-2.0",
  "packages": [
    "OpenMcdf@2.3.1"
  ]
}
{
  "license": "MS-PL",
  "packages": [
    "Svg.Custom@3.0.4"
  ]
}
{
  "license": "Unknown",
  "packages": [
    "Microsoft.NETCore.Platforms@1.1.1",
    "Microsoft.NETCore.Targets@1.1.3",
    "Microsoft.Web.WebView2@1.0.3351.48",
    "Microsoft.Windows.SDK.BuildTools@10.0.26100.4654",
    "Microsoft.WindowsAppSDK@1.7.250606001",
    "runtime.any.System.IO@4.3.0",
    "runtime.any.System.Reflection@4.3.0",
    "runtime.any.System.Reflection.Primitives@4.3.0",
    "runtime.any.System.Runtime@4.3.0",
    "runtime.any.System.Text.Encoding@4.3.0",
    "runtime.any.System.Threading.Tasks@4.3.0",
    "System.IO@4.3.0",
    "System.Private.Uri@4.3.2",
    "System.Reflection@4.3.0",
    "System.Reflection.Primitives@4.3.0",
    "System.Reflection.TypeExtensions@4.3.0",
    "System.Runtime@4.3.0",
    "System.Text.Encoding@4.3.0",
    "System.Threading.Tasks@4.3.0"
  ]
}
```
