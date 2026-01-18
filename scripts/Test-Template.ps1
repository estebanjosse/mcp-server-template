#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Tests the MCP Server template with various flag combinations.

.DESCRIPTION
    This script verifies that the dotnet template generates valid, buildable
    projects for common configuration scenarios.

.PARAMETER OutputPath
    Base directory for test outputs. Defaults to a temp directory.

.PARAMETER SkipCleanup
    If specified, test directories are not deleted after the run.

.EXAMPLE
    ./scripts/Test-Template.ps1
    
.EXAMPLE
    ./scripts/Test-Template.ps1 -OutputPath C:\temp\template-tests -SkipCleanup
#>

param(
    [string]$OutputPath = (Join-Path ([System.IO.Path]::GetTempPath()) "mcp-template-tests"),
    [switch]$SkipCleanup
)

$ErrorActionPreference = "Stop"

# Test scenarios: name, flags, should-build, should-test
$TestCases = @(
    @{ Name = "http-only"; Flags = "--http-host"; ShouldBuild = $true; ShouldTest = $false },
    @{ Name = "stdio-only"; Flags = "--stdio-host"; ShouldBuild = $true; ShouldTest = $false },
    @{ Name = "both-hosts"; Flags = "--http-host --stdio-host"; ShouldBuild = $true; ShouldTest = $false },
    @{ Name = "http-with-tests"; Flags = "--http-host --include-tests"; ShouldBuild = $true; ShouldTest = $true },
    @{ Name = "http-with-samples"; Flags = "--http-host --include-sample-tools"; ShouldBuild = $true; ShouldTest = $false },
    @{ Name = "full-http"; Flags = "--http-host --include-tests --include-sample-tools"; ShouldBuild = $true; ShouldTest = $true },
    @{ Name = "full-both"; Flags = "--http-host --stdio-host --include-tests --include-sample-tools"; ShouldBuild = $true; ShouldTest = $true }
)

function Write-TestHeader($message) {
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host $message -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}

function Write-TestResult($name, $success, $duration) {
    $status = if ($success) { "PASS" } else { "FAIL" }
    $color = if ($success) { "Green" } else { "Red" }
    Write-Host "[$status] $name ($([math]::Round($duration.TotalSeconds, 1))s)" -ForegroundColor $color
}

# Ensure output directory exists
if (Test-Path $OutputPath) {
    Remove-Item -Recurse -Force $OutputPath
}
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

Write-TestHeader "MCP Server Template Verification"
Write-Host "Output path: $OutputPath"
Write-Host "Test cases: $($TestCases.Count)"

$results = @()
$totalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()

foreach ($test in $TestCases) {
    $testPath = Join-Path $OutputPath $test.Name
    $testStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    Write-Host "`n--- Testing: $($test.Name) ---" -ForegroundColor Yellow
    Write-Host "Flags: $($test.Flags)"
    
    $success = $true
    $errorMessage = $null
    
    try {
        # Generate project
        Write-Host "  Creating project..." -NoNewline
        $createCmd = "dotnet new mcp-server $($test.Flags) -o `"$testPath`" --name Test.$($test.Name -replace '-','_')"
        $createResult = Invoke-Expression $createCmd 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Template creation failed: $createResult"
        }
        Write-Host " OK" -ForegroundColor Green
        
        # Build project
        if ($test.ShouldBuild) {
            Write-Host "  Building..." -NoNewline
            $buildResult = dotnet build $testPath --verbosity quiet 2>&1
            if ($LASTEXITCODE -ne 0) {
                throw "Build failed: $buildResult"
            }
            Write-Host " OK" -ForegroundColor Green
        }
        
        # Run tests
        if ($test.ShouldTest) {
            Write-Host "  Testing..." -NoNewline
            $testResult = dotnet test $testPath --verbosity quiet --no-build 2>&1
            if ($LASTEXITCODE -ne 0) {
                throw "Tests failed: $testResult"
            }
            Write-Host " OK" -ForegroundColor Green
        }
    }
    catch {
        $success = $false
        $errorMessage = $_.Exception.Message
        Write-Host " FAILED" -ForegroundColor Red
        Write-Host "  Error: $errorMessage" -ForegroundColor Red
    }
    
    $testStopwatch.Stop()
    $results += @{
        Name = $test.Name
        Success = $success
        Duration = $testStopwatch.Elapsed
        Error = $errorMessage
    }
}

$totalStopwatch.Stop()

# Summary
Write-TestHeader "Test Results Summary"

$passed = ($results | Where-Object { $_.Success }).Count
$failed = ($results | Where-Object { -not $_.Success }).Count

foreach ($result in $results) {
    Write-TestResult $result.Name $result.Success $result.Duration
}

Write-Host "`nTotal: $passed passed, $failed failed ($(([math]::Round($totalStopwatch.Elapsed.TotalSeconds, 1)))s)" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })

# Cleanup
if (-not $SkipCleanup) {
    Write-Host "`nCleaning up test directories..."
    Remove-Item -Recurse -Force $OutputPath -ErrorAction SilentlyContinue
}

# Exit with appropriate code
if ($failed -gt 0) {
    exit 1
}
