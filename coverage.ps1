<#
.SYNOPSIS
    Runs the Risebird unit-test suites with coverage and prints a per-project summary.

.DESCRIPTION
    Drives the three test projects, collects Cobertura output, and reports line and branch
    coverage for Risebird.Business, Risebird.CommonHelper and Risebird.Repository so progress
    toward the 100% target is measurable run over run.

    Requires the ReportGenerator global tool for the HTML report (optional):
        dotnet tool install --global dotnet-reportgenerator-globaltool

.EXAMPLE
    ./coverage.ps1
    ./coverage.ps1 -Html
#>
param(
    [switch]$Html,
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

Write-Host 'Cleaning previous coverage output...' -ForegroundColor Cyan
Get-ChildItem -Path $root -Directory -Recurse -Filter 'TestResults' -ErrorAction SilentlyContinue |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# The gate: Risebird.Business + Risebird.CommonHelper.
$gated = @(
    'Risebird.CommonHelper.Tests/Risebird.CommonHelper.Tests.csproj',
    'Risebird.Business.Tests/Risebird.Business.Tests.csproj'
)

# Informational only: Risebird.Repository tops out near 7% under unit tests by construction.
$informational = @{
    'Risebird.Repository.Tests/Risebird.Repository.Tests.csproj' = 'coverage.repository.runsettings'
}

foreach ($project in $gated) {
    Write-Host "`nRunning $project (gated)" -ForegroundColor Cyan
    dotnet test (Join-Path $root $project) `
        --configuration $Configuration `
        --settings (Join-Path $root 'coverage.runsettings') `
        --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  tests failed in $project (coverage below is still valid)" -ForegroundColor Yellow
    }
}

foreach ($project in $informational.Keys) {
    Write-Host "`nRunning $project (informational)" -ForegroundColor DarkCyan
    dotnet test (Join-Path $root $project) `
        --configuration $Configuration `
        --settings (Join-Path $root $informational[$project]) `
        --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  tests failed in $project (coverage below is still valid)" -ForegroundColor Yellow
    }
}

$reports = Get-ChildItem -Path $root -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue
if (-not $reports) {
    Write-Host "`nNo coverage files produced." -ForegroundColor Red
    exit 1
}

Write-Host "`n=================== COVERAGE BY PROJECT ===================" -ForegroundColor Green

$totals = @{}
foreach ($report in $reports) {
    [xml]$xml = Get-Content $report.FullName
    foreach ($package in $xml.coverage.packages.package) {
        $name = $package.name
        if (-not $totals.ContainsKey($name)) {
            $totals[$name] = [pscustomobject]@{
                Name = $name; LinesCovered = 0; LinesValid = 0
                BranchesCovered = 0; BranchesValid = 0
            }
        }
        foreach ($class in $package.classes.class) {
            foreach ($line in $class.lines.line) {
                $totals[$name].LinesValid++
                if ([int]$line.hits -gt 0) { $totals[$name].LinesCovered++ }
                if ($line.branch -eq 'True' -and $line.'condition-coverage') {
                    if ($line.'condition-coverage' -match '\((\d+)/(\d+)\)') {
                        $totals[$name].BranchesCovered += [int]$Matches[1]
                        $totals[$name].BranchesValid   += [int]$Matches[2]
                    }
                }
            }
        }
    }
}

$gateProjects = @('Risebird.Business', 'Risebird.CommonHelper')

$totals.Values | Sort-Object Name | ForEach-Object {
    $linePct   = if ($_.LinesValid    -gt 0) { 100.0 * $_.LinesCovered    / $_.LinesValid }    else { 0 }
    $branchPct = if ($_.BranchesValid -gt 0) { 100.0 * $_.BranchesCovered / $_.BranchesValid } else { 0 }
    $isGated = $gateProjects -contains $_.Name
    $tag = if ($isGated) { '' } else { '   [informational]' }
    $colour = if (-not $isGated) { 'DarkGray' }
              elseif ($linePct -ge 90) { 'Green' }
              elseif ($linePct -ge 60) { 'Yellow' }
              else { 'Red' }
    Write-Host ("{0,-28} lines {1,6:N2}%  ({2}/{3})   branches {4,6:N2}%  ({5}/{6}){7}" -f `
        $_.Name, $linePct, $_.LinesCovered, $_.LinesValid, $branchPct, $_.BranchesCovered, $_.BranchesValid, $tag) `
        -ForegroundColor $colour
}

Write-Host ''
Write-Host 'Gate = Risebird.Business + Risebird.CommonHelper. Risebird.Repository is reported for' -ForegroundColor DarkGray
Write-Host 'information only; its unit-test ceiling is ~7% by construction (see COVERAGE_PLAN.md).' -ForegroundColor DarkGray

if ($Html) {
    Write-Host "`nGenerating HTML report..." -ForegroundColor Cyan
    $inputs = ($reports | ForEach-Object { $_.FullName }) -join ';'
    reportgenerator "-reports:$inputs" "-targetdir:$root/TestResults/CoverageReport" '-reporttypes:Html;TextSummary'
    Write-Host "Report: $root/TestResults/CoverageReport/index.html" -ForegroundColor Green
}
