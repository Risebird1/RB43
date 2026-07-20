<#
.SYNOPSIS
    Creates or updates both Azure AI Search indexes: raisedissueindex and templateindex.

.DESCRIPTION
    Wrapper that runs:
      - Create-RaisedIssueIndex.ps1
      - Create-TemplateIndex.ps1

    After indexes are created, seed data with FullCrawl on the Function App:
      POST /api/RaisedIssueIndexSearch/FullCrawl?code=<key>
      POST /api/TemplateIndexSearch/FullCrawl?code=<key>

    Prerequisites:
      - Deploy usp_GetRaisedIssueIndexBatch and usp_GetTemplateIndexBatch
      - Set AzureSearch:RaisedIssueListing:Enabled=true
      - Set AzureSearch:TemplateListing:Enabled=true

.PARAMETER SearchServiceName
    Azure AI Search service name (without .search.windows.net).

.PARAMETER SearchEndpoint
    Full endpoint URL. Used when SearchServiceName is not supplied.

.PARAMETER AdminApiKey
    Admin API key for the search service.

.PARAMETER RaisedIssueIndexName
    Index name for raised issues. Default: raisedissueindex

.PARAMETER TemplateIndexName
    Index name for templates. Default: templateindex

.PARAMETER ApiVersion
    Azure Search REST API version.

.PARAMETER Recreate
    Deletes each index before creating it. All documents are removed.

.EXAMPLE
    cd "c:\Users\prem\source\repos\Risebird\Risebird.DB\Scripts"

    .\Create-RaisedIssueAndTemplateIndexes.ps1 `
        -SearchEndpoint "https://rb-ci-index.search.windows.net" `
        -AdminApiKey "<admin-key>"

.EXAMPLE
    .\Create-RaisedIssueAndTemplateIndexes.ps1 `
        -SearchEndpoint "https://rb-ci-index.search.windows.net" `
        -AdminApiKey "<admin-key>" `
        -Recreate
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $SearchServiceName,

    [Parameter(Mandatory = $false)]
    [string] $SearchEndpoint,

    [Parameter(Mandatory = $true)]
    [string] $AdminApiKey,

    [Parameter(Mandatory = $false)]
    [string] $RaisedIssueIndexName = "raisedissueindex",

    [Parameter(Mandatory = $false)]
    [string] $TemplateIndexName = "templateindex",

    [Parameter(Mandatory = $false)]
    [string] $ApiVersion = "2024-07-01",

    [Parameter(Mandatory = $false)]
    [switch] $Recreate
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Invoke-ChildIndexScript {
    param(
        [string] $ScriptName,
        [string] $IndexName
    )

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " $ScriptName" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    $childParams = @{
        AdminApiKey = $AdminApiKey
        IndexName   = $IndexName
        ApiVersion  = $ApiVersion
    }

    if (-not [string]::IsNullOrWhiteSpace($SearchEndpoint)) {
        $childParams["SearchEndpoint"] = $SearchEndpoint
    }
    elseif (-not [string]::IsNullOrWhiteSpace($SearchServiceName)) {
        $childParams["SearchServiceName"] = $SearchServiceName
    }
    else {
        throw "Provide either -SearchEndpoint or -SearchServiceName."
    }

    if ($Recreate) {
        $childParams["Recreate"] = $true
    }

    & (Join-Path $scriptDir $ScriptName) @childParams
}

Invoke-ChildIndexScript -ScriptName "Create-RaisedIssueIndex.ps1" -IndexName $RaisedIssueIndexName
Invoke-ChildIndexScript -ScriptName "Create-TemplateIndex.ps1" -IndexName $TemplateIndexName

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Both indexes provisioned" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Raised issue index : $RaisedIssueIndexName"
Write-Host "Template index     : $TemplateIndexName"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Enable listing flags on Web + Function App:"
Write-Host "       AzureSearch:RaisedIssueListing:Enabled=true"
Write-Host "       AzureSearch:TemplateListing:Enabled=true"
Write-Host "  2. Full crawl (Function App):"
Write-Host "       POST https://<function-app>/api/RaisedIssueIndexSearch/FullCrawl?code=<key>"
Write-Host "       POST https://<function-app>/api/TemplateIndexSearch/FullCrawl?code=<key>"
