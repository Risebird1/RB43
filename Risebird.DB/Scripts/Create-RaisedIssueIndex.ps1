<#
.SYNOPSIS
    Creates or updates the Azure AI Search raised-issue support ticket index (raisedissueindex).

.DESCRIPTION
    Provisions the index schema used by:
      - Risebird.Repository/Indexing/RaisedIssueIndexFields.cs
      - Risebird.Repository/Indexing/RaisedIssueIndexDefinition.cs
      - Risebird.Repository/Indexing/RaisedIssueIndexDocumentMapper.cs
      - Risebird.Entities/Search/RaisedIssueIndexDocument.cs
      - usp_GetRaisedIssueIndexBatch

    After creating the index, load data via:
      Full crawl : POST /api/RaisedIssueIndexSearch/FullCrawl?code=<key>
      Single doc : POST /api/RaisedIssueIndexSearch/Upsert?raisedIssueId=<id>&code=<key>
    Or raise/close/reopen a ticket in the portal (automatic upsert).

.PARAMETER SearchServiceName
    Azure AI Search service name (without .search.windows.net).

.PARAMETER SearchEndpoint
    Full endpoint URL. Used when SearchServiceName is not supplied.

.PARAMETER AdminApiKey
    Admin API key for the search service.

.PARAMETER IndexName
    Index name. Default: raisedissueindex

.PARAMETER ApiVersion
    Azure Search REST API version.

.PARAMETER Recreate
    Deletes the existing index before creating it. All documents are removed.

.EXAMPLE
    .\Create-RaisedIssueIndex.ps1 `
        -SearchEndpoint "https://rb-ci-index.search.windows.net" `
        -AdminApiKey "<admin-key>"
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
    [string] $IndexName = "raisedissueindex",

    [Parameter(Mandatory = $false)]
    [string] $ApiVersion = "2024-07-01",

    [Parameter(Mandatory = $false)]
    [switch] $Recreate
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

function Get-RaisedIssueIndexDefinition {
    param([string] $Name)

    return [ordered]@{
        name   = $Name
        fields = @(
            (New-SearchField -Name "id" -Type "Edm.String" -Key $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "requestType" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "comment" -Type "Edm.String" -Searchable $true)
            (New-SearchField -Name "attachmentURL" -Type "Edm.String")
            (New-SearchField -Name "requestedBy" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "requestedOn" -Type "Edm.DateTimeOffset" -Filterable $true -Sortable $true)
            (New-SearchField -Name "status" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "statusComment" -Type "Edm.String")
            (New-SearchField -Name "closedBy" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "closedOn" -Type "Edm.DateTimeOffset" -Filterable $true -Sortable $true)
            (New-SearchField -Name "requester" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "interviewId" -Type "Edm.Int32" -Filterable $true)
            (New-SearchField -Name "aiReviewResponse" -Type "Edm.String")
            (New-SearchField -Name "mobileNumber" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "name" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "irfIntId" -Type "Edm.Int32" -Filterable $true)
            (New-SearchField -Name "candidateEmail" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "interviewer" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "isReopened" -Type "Edm.Boolean" -Filterable $true)
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

$indexDefinition = Get-RaisedIssueIndexDefinition -Name $IndexName

Write-Host "Creating/updating index..."
$result = Invoke-SearchRest -Method "PUT" -Url $indexUrl -Body $indexDefinition

Write-Host ""
Write-Host "Index provisioned successfully."
Write-Host "  Name   : $($result.name)"
Write-Host "  Fields : $($result.fields.Count)"
Write-Host ""
Write-Host "Load documents:"
Write-Host "  1. Full crawl  -> POST https://<function-app>/api/RaisedIssueIndexSearch/FullCrawl?code=<key>"
Write-Host "     (Also: /api/ListingIndex/RaisedIssueListing/FullCrawl)"
Write-Host "  2. Single doc  -> POST https://<function-app>/api/RaisedIssueIndexSearch/Upsert?raisedIssueId=<id>&code=<key>"
Write-Host "     (Also: /api/ListingIndex/RaisedIssueListing/Upsert?documentId=<id>&code=<key>)"
Write-Host "  3. Portal      -> Raise, close, reopen, or AI-review a ticket (auto upsert on save)"
Write-Host ""
Write-Host "App settings required:"
Write-Host "  AzureSearch:Endpoint, AzureSearch:ApiKey"
Write-Host "  AzureSearch:RaisedIssueListing:Enabled=true"
Write-Host "  AzureSearch:RaisedIssueListing:IndexName=$IndexName"
Write-Host "  AzureSearch:RaisedIssueListing:ManageIndexSchema=false"
