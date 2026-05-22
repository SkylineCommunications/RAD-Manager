    param (
        [Parameter(Mandatory = $true)]
        [string]$PathToTestPackageContent
    )

    $ErrorActionPreference = 'Stop'

    try {
        # Import common code module
        Import-Module -Name (Join-Path $PSScriptRoot 'CommonCode.psm1')

        # 0) Install the dotnet tool first
        Write-Host "Installing dotnet tool: skyline.dataminer.cicd.tools.runautomationscript..." -ForegroundColor Cyan
        dotnet tool install skyline.dataminer.cicd.tools.runautomationscript --create-manifest-if-needed --add-source https://api.nuget.org/v3/index.json
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to install skyline.dataminer.cicd.tools.runautomationscript."
        }

        function Copy-DirWithRobocopy {
            param(
                [Parameter(Mandatory=$true)][string]$Source,
                [Parameter(Mandatory=$true)][string]$Destination
            )

            if (-not (Test-Path -Path $Source)) {
                Write-Warning "Source not found, skipping: $Source"
                return
            }

            if (-not (Test-Path -Path $Destination)) {
                New-Item -ItemType Directory -Force -Path $Destination | Out-Null
            }

            # /E = subdirs incl empty, /R /W = retries, and quiet switches
            $null = robocopy $Source $Destination *.* /E /R:3 /W:5 /NFL /NDL /NJH /NJS /NC /NS /NP
            $rc = $LASTEXITCODE

            # Robocopy exit codes: 0–7 => success; >=8 => failure
            if ($rc -ge 8) {
                throw "RoboCopy failed copying `"$Source`" to `"$Destination`" with exit code $rc"
            }
        }

        $radPlaywrightTests = 'C:\RADPlaywrightTests'

        # Ensure the RADPlaywrightTests folder exists
        if (-not (Test-Path -Path $radPlaywrightTests)) {
            Write-Host "Creating directory: $radPlaywrightTests" -ForegroundColor Cyan
            New-Item -Path $radPlaywrightTests -ItemType Directory -Force | Out-Null
        }

        # 1) Locate TestHarvesting\dependencies.generated
        $testHarvestingPath = Join-Path $PathToTestPackageContent 'TestHarvesting'
        $dependenciesPath   = Join-Path $testHarvestingPath 'dependencies.generated'
        $testsGeneratedPath = Join-Path $testHarvestingPath 'tests.generated'

        # 2) Copy dependencies.generated folder to RADPlaywrightTests
        if (Test-Path -Path $dependenciesPath) {
            Write-Host "Copying dependencies.generated to $radPlaywrightTests..." -ForegroundColor Cyan
            $destDependencies = Join-Path $radPlaywrightTests 'dependencies.generated'
            Copy-DirWithRobocopy -Source $dependenciesPath -Destination $destDependencies
        } else {
            Write-Warning "dependencies.generated folder not found at: $dependenciesPath"
        }

        # 3) Copy tests.generated folder to RADPlaywrightTests
        if (Test-Path -Path $testsGeneratedPath) {
            Write-Host "Copying tests.generated to $radPlaywrightTests..." -ForegroundColor Cyan
            $destTests = Join-Path $radPlaywrightTests 'tests.generated'
            Copy-DirWithRobocopy -Source $testsGeneratedPath -Destination $destTests
        } else {
            Write-Warning "tests.generated folder not found at: $testsGeneratedPath"
        }

        Write-Host "Test package setup completed successfully." -ForegroundColor Green
    }
    catch {
        Write-Host "Error occurred during test package setup: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Red
        throw
    }


