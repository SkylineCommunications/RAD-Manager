$ErrorActionPreference = 'Stop'

# Base path four levels up, cross-platform
$pathToSolutionRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..\')

$pathToGeneratedTests = Join-Path $PSScriptRoot 'tests.generated'
$pathToGeneratedDependencies = Join-Path $PSScriptRoot 'dependencies.generated'
$pathToXmlAutomationTests = Join-Path $PSScriptRoot 'xmlautomationtests.generated'

# Clean up any previous output
if (Test-Path $pathToGeneratedTests) {
    Remove-Item -Recurse -Force $pathToGeneratedTests
}

if (Test-Path $pathToGeneratedDependencies) {
    Remove-Item -Recurse -Force $pathToGeneratedDependencies
}

if (Test-Path $pathToXmlAutomationTests) {
    Remove-Item -Recurse -Force $pathToXmlAutomationTests
}

New-Item -ItemType Directory -Force -Path $pathToGeneratedTests  | Out-Null
New-Item -ItemType Directory -Force -Path $pathToGeneratedDependencies  | Out-Null
New-Item -ItemType Directory -Force -Path $pathToXmlAutomationTests  | Out-Null

$radTestsPath = Join-Path $pathToSolutionRoot 'RADPlaywright'
Write-Host "Looking for top-level .cs files in solution folder: $radTestsPath" -ForegroundColor Cyan

$topLevelCsFiles = Get-ChildItem -Path $radTestsPath -File -Filter '*.cs'

foreach ($file in $topLevelCsFiles) {
    Copy-Item $file.FullName (Join-Path $pathToGeneratedTests $file.Name) -Force
}

Write-Host "Copied $($topLevelCsFiles.Count) top-level .cs file(s)." -ForegroundColor Cyan

# Copy build output files
$buildOutputPath = Join-Path $radTestsPath 'bin\Debug\net48'
Write-Host "Looking for build output files in: $buildOutputPath" -ForegroundColor Cyan

if (Test-Path $buildOutputPath) {
    # Copy entire net48 directory
    Copy-Item -Path $buildOutputPath -Destination $pathToGeneratedDependencies -Recurse -Force
    
    # Count all files copied (including subdirectories)
    $copiedCount = (Get-ChildItem -Path (Join-Path $pathToGeneratedDependencies 'net48') -Recurse -File).Count
    
    Write-Host "Copied entire net48 folder with $copiedCount file(s)." -ForegroundColor Cyan
} else {
    Write-Warning "Build output path not found: $buildOutputPath"
    Write-Warning "Please ensure the project has been built in Debug configuration for net48"
}

# Warning, do not cleanup the collected files here. Next step in the SDK will use these.
Write-Information "`n🎉 Script completed successfully!"
exit 0