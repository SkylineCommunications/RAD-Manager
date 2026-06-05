$ErrorActionPreference = 'Stop'

# Import common code module
Import-Module -Name (Join-Path $PSScriptRoot 'CommonCode.psm1')

# Track script start time
$scriptStart = Get-Date

try {
    Write-Host "Running Test Package tests..." -ForegroundColor Cyan
    
    # Define Playwright directory
    $PlaywrightDir = 'C:\RADPlaywrightTests\dependencies.generated\net48'
    
    if (-not (Test-Path $PlaywrightDir)) {
        throw "Playwright directory not found: $PlaywrightDir"
    }
    
    Write-Host "Setting up Playwright environment in $PlaywrightDir..." -ForegroundColor Cyan
    Set-Location $PlaywrightDir
    
    # Create tool manifest (force to avoid conflicts)
    Write-Host "Creating tool manifest..." -ForegroundColor Cyan
    dotnet new tool-manifest --force | Out-Null
    
    # Install Playwright CLI as local tool
    Write-Host "Installing Microsoft.Playwright.CLI..." -ForegroundColor Cyan
    dotnet tool install Microsoft.Playwright.CLI --local
    
    # Run Playwright browser installation using the generated PowerShell script
    Write-Host "Installing Playwright browsers..." -ForegroundColor Cyan
    $playwrightPs1 = Get-ChildItem -Path $PlaywrightDir -Recurse -Filter "playwright.ps1" -ErrorAction SilentlyContinue | Select-Object -First 1

    if ($null -eq $playwrightPs1) {
        throw "playwright.ps1 not found in $PlaywrightDir. Ensure the project has been built."
    }

    Write-Host "Using playwright.ps1 from: $($playwrightPs1.FullName)" -ForegroundColor Yellow
    & $playwrightPs1.FullName install

    # Set Playwright driver path - the driver is in the same directory as playwright.ps1
    $playwrightDriverDir = Split-Path $playwrightPs1.FullName -Parent
    $env:PLAYWRIGHT_DRIVER_PATH = $playwrightDriverDir
    Write-Host "Set PLAYWRIGHT_DRIVER_PATH to: $playwrightDriverDir" -ForegroundColor Cyan

    Write-Host "Starting Playwright test..." -ForegroundColor Cyan

    # Find the test DLL in the Playwright directory
    $testDllPath = Get-ChildItem -Path $PlaywrightDir -Recurse -Filter "RADPlaywright.dll" -ErrorAction SilentlyContinue | 
        Select-Object -First 1

    if ($null -eq $testDllPath) {
        Write-Host "Test DLL not found in $PlaywrightDir" -ForegroundColor Red
        Write-Host "Searching for any test DLLs..." -ForegroundColor Yellow

        Get-ChildItem -Path $PlaywrightDir -Recurse -Filter "*.dll" | 
            Where-Object { $_.Name -like "*Playwright*.dll" } | 
            ForEach-Object { Write-Host "  Found: $($_.FullName)" -ForegroundColor Yellow }

        throw "RADPlaywright.dll not found in $PlaywrightDir"
    }

    $publishedTestDll = $testDllPath.FullName
    Write-Host "Running tests from: $publishedTestDll" -ForegroundColor Yellow

    # Create a unique TRX file name
    $trxFileName = "TestResults_$(Get-Date -Format 'yyyyMMdd_HHmmss').trx"
    $trxFilePath = Join-Path $PlaywrightDir $trxFileName

    $env:PLAYWRIGHT_DEBUG = "1"

    # Then run the test command with these additional loggers:
    $playwrightOutput = dotnet test $publishedTestDll `
        --logger "trx;LogFileName=$trxFilePath" `
        --logger "console;verbosity=detailed" `
        -- NUnit.ShowStackTraces=true 2>&1    

    $playwrightExitCode = $LASTEXITCODE

    $playwrightMessage = ($playwrightOutput | Out-String).Trim()

    Write-Host "Playwright output:" -ForegroundColor DarkGray
    Write-Host $playwrightMessage

    # Check if TRX file was created
    if (Test-Path $trxFilePath) {
        Write-Host "Test results saved to: $trxFilePath" -ForegroundColor Cyan
    }

    if ($playwrightExitCode -eq 0) {
        if ([string]::IsNullOrWhiteSpace($playwrightMessage)) { 
            $playwrightMessage = "Playwright UI test passed." 
        }

        Write-Host "Playwright UI test SUCCEEDED." -ForegroundColor Green

        try { Push-TestCaseResult -Outcome 'OK' -Name 'RADManager_PlaywrightUITest' -Duration ((Get-Date) - $scriptStart) -Message $playwrightMessage -TestAspect Assertion } catch {}
    }
    else {
        if ([string]::IsNullOrWhiteSpace($playwrightMessage)) { 
            $playwrightMessage = "Playwright UI test failed." 
        }

        Write-Host "Playwright UI test FAILED." -ForegroundColor Red

        try { Push-TestCaseResult -Outcome 'Fail' -Name 'RADManager_PlaywrightUITest' -Duration ((Get-Date) - $scriptStart) -Message $playwrightMessage -TestAspect Assertion } catch {}

        throw "Playwright UI test failed with exit code $playwrightExitCode."
    }

    Write-Host "Test Package execution finished successfully." -ForegroundColor Green

    try { Push-TestCaseResult -Outcome 'OK' -Name 'RADManager_TestPackageExecution' -Duration ((Get-Date) - $scriptStart) -Message 'Test Package execution finished.' -TestAspect Execution } catch {}
}
catch {
    Write-Host "Test Package execution FAILED: $($_.Exception.Message)" -ForegroundColor Red

    try { Push-TestCaseResult -Outcome 'Fail' -Name 'RADManager_TestPackageExecution' -Duration ((Get-Date) - $scriptStart) -Message "Exception during Test Package execution: $($_.Exception.Message)" -TestAspect Execution } catch {}

    exit 1
}