<#
.SYNOPSIS
    Creates or updates the Azure AI Search call catalogue index (callcatalogueindex).

.DESCRIPTION
    Provisions the index schema used by:
      - Risebird.Repository/Indexing/CallCatalogueIndexFields.cs
      - Risebird.Repository/Indexing/CallCatalogueIndexDefinition.cs
      - Risebird.Repository/Indexing/CallCatalogueIndexDocumentMapper.cs
      - usp_GetCallCatalogueIndexBatch (SQL source)

    UI: /HR/CallCatalogue

    After creating the index, load data via:
      Full crawl : POST /api/CallCatalogueIndexSearch/FullCrawl?code=<key>
      Candidate  : POST /api/CallCatalogueIndexSearch/UpsertCandidate?irfId=<id>&candidateEmail=<email>&code=<key>
      Interviewer: POST /api/CallCatalogueIndexSearch/UpsertInterviewer?interviewerEmail=<email>&code=<key>

.PARAMETER SearchServiceName
    Azure AI Search service name (without .search.windows.net).

.PARAMETER SearchEndpoint
    Full endpoint URL. Used when SearchServiceName is not supplied.

.PARAMETER AdminApiKey
    Admin API key for the search service.

.PARAMETER IndexName
    Index name. Default: callcatalogueindex

.PARAMETER ApiVersion
    Azure Search REST API version.

.PARAMETER Recreate
    Deletes the existing index before creating it. All documents are removed.

.PARAMETER FunctionAppUrl
    Optional. Azure Function App base URL (e.g. https://my-func.azurewebsites.net).
    When set with -FunctionKey, triggers a full crawl after index creation.

.PARAMETER FunctionKey
    Optional. Function host key for CallCatalogueIndexSearchFullCrawl.

.EXAMPLE
    cd "C:\Users\prem\Source\Repos\Risebird\Risebird.DB\Scripts"

    .\Create-CallCatalogueIndex.ps1 `
        -SearchEndpoint "https://rb-ci-index.search.windows.net" `
        -AdminApiKey "<admin-key>"

.EXAMPLE
    .\Create-CallCatalogueIndex.ps1 `
        -SearchEndpoint "https://rb-ci-index.search.windows.net" `
        -AdminApiKey "<admin-key>" `
        -FunctionAppUrl "https://risebird-timers.azurewebsites.net" `
        -FunctionKey "<function-key>"

.EXAMPLE
    .\Create-CallCatalogueIndex.ps1 `
        -SearchServiceName "rb-ci-index" `
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
    [string] $IndexName = "callcatalogueindex",

    [Parameter(Mandatory = $false)]
    [string] $ApiVersion = "2024-07-01",

    [Parameter(Mandatory = $false)]
    [switch] $Recreate,

    [Parameter(Mandatory = $false)]
    [string] $FunctionAppUrl,

    [Parameter(Mandatory = $false)]
    [string] $FunctionKey
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-SearchEndpoint {
    if (-not [string]::IsNullOrWhiteSpace($SearchEndpoint)) {
        return $SearchEndpoint.TrimEnd("/")
    }
    if ([string]::IsNullOrWhiteSpace($SearchServiceName)) {
        throw "Provide either -SearchEndpoint or -SearchServiceName."
    }
    return "https://$SearchServiceName.search.windows.net"
}

function New-SearchField {
    param(
        [string] $Name,
        [string] $Type,
        [bool] $Key = $false,
        [bool] $Searchable = $false,
        [bool] $Filterable = $false,
        [bool] $Sortable = $false,
        [bool] $Facetable = $false,
        [bool] $Retrievable = $true
    )

    return [ordered]@{
        name        = $Name
        type        = $Type
        key         = $Key
        searchable  = $Searchable
        filterable  = $Filterable
        sortable    = $Sortable
        facetable   = $Facetable
        retrievable = $Retrievable
    }
}

# Must match Risebird.Repository.Indexing.CallCatalogueIndexDefinition field flags.
function Get-CallCatalogueIndexDefinition {
    param([string] $Name)

    return [ordered]@{
        name   = $Name
        fields = @(
            (New-SearchField -Name "id" -Type "Edm.String" -Key $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "catalogueType" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "irfIntId" -Type "Edm.Int32" -Filterable $true)
            (New-SearchField -Name "irfId" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "candidateEmail" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "candidateName" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "jdTitle" -Type "Edm.String" -Searchable $true)
            (New-SearchField -Name "primarySkill" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "clientName" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "calledOn" -Type "Edm.DateTimeOffset" -Filterable $true -Sortable $true)
            (New-SearchField -Name "whoCalled" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "allowAllInternal" -Type "Edm.Boolean" -Filterable $true)
            (New-SearchField -Name "allowedViewerEmails" -Type "Collection(Edm.String)" -Filterable $true)
        )
    }
}

function Invoke-SearchRest {
    param(
        [string] $Method,
        [string] $Url,
        [object] $Body = $null
    )

    $headers = @{
        "Content-Type" = "application/json"
        "api-key"      = $AdminApiKey
    }

    $params = @{
        Method  = $Method
        Uri     = $Url
        Headers = $headers
    }

    if ($null -ne $Body) {
        $params["Body"] = ($Body | ConvertTo-Json -Depth 20)
    }

    return Invoke-RestMethod @params
}

$endpoint = Resolve-SearchEndpoint
$indexUrl = "$endpoint/indexes/$IndexName`?api-version=$ApiVersion"

Write-Host "Search endpoint : $endpoint"
Write-Host "Index name      : $IndexName"
Write-Host "API version     : $ApiVersion"

if ($Recreate) {
    Write-Warning "Recreate requested. Deleting index '$IndexName' if it exists..."
    try {
        Invoke-SearchRest -Method "DELETE" -Url $indexUrl | Out-Null
        Write-Host "Existing index deleted."
    }
    catch {
        Write-Host "No existing index to delete (or delete failed): $($_.Exception.Message)"
    }
}

Write-Host "Creating/updating index..."
$result = Invoke-SearchRest -Method "PUT" -Url $indexUrl -Body (Get-CallCatalogueIndexDefinition -Name $IndexName)

Write-Host ""
Write-Host "Index provisioned successfully."
Write-Host "  Name   : $($result.name)"
Write-Host "  Fields : $($result.fields.Count)"
Write-Host ""
Write-Host "App settings required:"
Write-Host "  AzureSearch:Endpoint, AzureSearch:ApiKey"
Write-Host "  AzureSearch:CallCatalogueListing:Enabled=true"
Write-Host "  AzureSearch:CallCatalogueListing:IndexName=$IndexName"
Write-Host "  AzureSearch:CallCatalogueListing:ManageIndexSchema=false"
Write-Host ""

if (-not [string]::IsNullOrWhiteSpace($FunctionAppUrl) -and -not [string]::IsNullOrWhiteSpace($FunctionKey)) {
    $crawlUrl = "$($FunctionAppUrl.TrimEnd('/'))/api/CallCatalogueIndexSearch/FullCrawl?code=$FunctionKey"
    Write-Host "Triggering full crawl: $crawlUrl"
    try {
        $crawlResponse = Invoke-RestMethod -Method Post -Uri $crawlUrl
        Write-Host "Full crawl response: $crawlResponse"
    }
    catch {
        Write-Warning "Full crawl request failed: $($_.Exception.Message)"
        Write-Host "Run manually: POST $crawlUrl"
    }
}
else {
    Write-Host "Load documents:"
    Write-Host "  1. Full crawl   -> POST https://<function-app>/api/CallCatalogueIndexSearch/FullCrawl?code=<key>"
    Write-Host "  2. One candidate-> POST .../UpsertCandidate?irfId=<id>&candidateEmail=<email>&code=<key>"
    Write-Host "  3. One interviewer -> POST .../UpsertInterviewer?interviewerEmail=<email>&code=<key>"
    Write-Host ""
    Write-Host "Or re-run with -FunctionAppUrl and -FunctionKey to crawl automatically."
}
