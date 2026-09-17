param(
    [Parameter(Mandatory)][string]$PackageDirectory,
    [string]$DotNet = "dotnet"
)
$ErrorActionPreference = "Stop"
$packages = @(Get-ChildItem -LiteralPath $PackageDirectory -Filter "BrighterTools.CodeGenerator.*.nupkg")
if ($packages.Count -ne 1) { throw "Supply a directory containing exactly one generator package." }
$version = $packages[0].Name -replace '^BrighterTools\.CodeGenerator\.', '' -replace '\.nupkg$', ''
$root = Join-Path ([IO.Path]::GetTempPath()) ("brightertools-packed-smoke-" + [guid]::NewGuid().ToString("N"))
$null = New-Item -ItemType Directory -Path $root
Copy-Item -Path (Join-Path $PSScriptRoot "fixtures/packed-tool/*") -Destination $root -Recurse
$feed = Join-Path $root "feed"
$null = New-Item -ItemType Directory -Path $feed
Copy-Item -LiteralPath $packages[0].FullName -Destination $feed
# A local-only feed and isolated cache guarantee we test this exact artifact,
# not an already published package with the same (deliberately unchanged) version.
$configPath = Join-Path $root "NuGet.config"
$config = [xml]'<configuration><packageSources><clear/><add key="smoke" value=""/></packageSources></configuration>'
$config.configuration.packageSources.add.value = $feed
$config.Save($configPath)
$previousPackages = $env:NUGET_PACKAGES
try {
    $env:NUGET_PACKAGES = Join-Path $root "packages"
    $toolPath = Join-Path $root "tools"
    & $DotNet tool install BrighterTools.CodeGenerator --tool-path $toolPath --version $version --configfile $configPath
    if ($LASTEXITCODE -ne 0) { throw "Tool installation failed." }
    & $DotNet restore (Join-Path $root "App/App.csproj") --configfile $configPath
    if ($LASTEXITCODE -ne 0) { throw "Fixture restore failed." }
    $tool = Join-Path $toolPath "brightertools-codegenerator"
    if (Test-Path ($tool + ".exe")) { $tool += ".exe" }
    $argsForTool = @("generate", "--config", (Join-Path $root "CodeGeneration/codegen.json"))
    & $tool @argsForTool
    if ($LASTEXITCODE -ne 0) { throw "Packed tool generation failed." }
    $files = @(Get-ChildItem -LiteralPath (Join-Path $root "App") -Recurse -Filter "*.g.cs" |
        Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' })
    if ($files.Count -lt 2) { throw "Expected repository and data registration output." }
    $registration = $files | Where-Object { [IO.File]::ReadAllText($_.FullName).Contains("UseSqlServer") }
    if (@($registration).Count -ne 1) { throw "Expected exactly one SQL Server registration." }
    $sql = [IO.File]::ReadAllText($registration.FullName)
    foreach ($expected in @("DefaultConnection", "EnableRetryOnFailure", "SplitQuery", "WidgetRepository")) {
        if (-not $sql.Contains($expected)) { throw "Missing default SQL Server behavior: $expected" }
    }
    if ($sql.Contains("UseSqlite") -or $sql.Contains("Database:Provider")) { throw "Unexpected provider-switch regression." }
    $hashes = @{}
    foreach ($file in $files) { $hashes[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
    & $tool @argsForTool
    if ($LASTEXITCODE -ne 0) { throw "Repeat generation failed." }
    foreach ($file in $files) {
        if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne $hashes[$file.FullName]) {
            throw "Generation is not deterministic: $($file.Name)"
        }
    }
    Write-Output "Packed-tool smoke test passed: $($files.Count) generated files; artifacts retained at $root"
} finally {
    $env:NUGET_PACKAGES = $previousPackages
}
