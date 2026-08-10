<#
.SYNOPSIS
    Creates or updates the Azure AI Search template (ClonableSkillChannel) index (templateindex).

.DESCRIPTION
    Provisions the index schema used by:
      - Risebird.Repository/Indexing/TemplateIndexFields.cs
      - Risebird.Repository/Indexing/TemplateIndexDefinition.cs
      - Risebird.Repository/Indexing/TemplateIndexDocumentMapper.cs
      - Risebird.Entities/Search/TemplateIndexDocument.cs
      - usp_GetTemplateIndexBatch

    After creating the index, load data via:
      Full crawl : POST /api/TemplateIndexSearch/FullCrawl?code=<key>
      Single doc : POST /api/TemplateIndexSearch/Upsert?clonableSkillChannelId=<id>&code=<key>
    Or create/update/delete/restore a template in the portal (automatic upsert).

.PARAMETER SearchServiceName
    Azure AI Search service name (without .search.windows.net).

.PARAMETER SearchEndpoint
    Full endpoint URL. Used when SearchServiceName is not supplied.

.PARAMETER AdminApiKey
    Admin API key for the search service.

.PARAMETER IndexName
    Index name. Default: templateindex

.PARAMETER ApiVersion
    Azure Search REST API version.

.PARAMETER Recreate
    Deletes the existing index before creating it. All documents are removed.

.EXAMPLE
    .\Create-TemplateIndex.ps1 `
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
    [string] $IndexName = "templateindex",

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

function Get-TemplateIndexDefinition {
    param([string] $Name)

    return [ordered]@{
        name   = $Name
        fields = @(
            (New-SearchField -Name "clonableSkillChannelId" -Type "Edm.String" -Key $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "name" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "createdFromIRFId" -Type "Edm.Int32" -Filterable $true)
            (New-SearchField -Name "meetingPlatform" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "companyTypeId" -Type "Edm.Int32" -Filterable $true)
            (New-SearchField -Name "seniorityLevelId" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "countryType" -Type "Edm.Int32" -Filterable $true)
            (New-SearchField -Name "assessmentGroupName" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "assessmentGroupId" -Type "Edm.Int32" -Filterable $true)
            (New-SearchField -Name "difficultyLevel" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "employmentType" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "perHourRate" -Type "Edm.Int32" -Filterable $true)
            (New-SearchField -Name "categoryName" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "skillChannelCounts" -Type "Edm.Int32" -Filterable $true -Sortable $true)
            (New-SearchField -Name "primarySkills" -Type "Edm.String" -Searchable $true)
            (New-SearchField -Name "createdOn" -Type "Edm.DateTimeOffset" -Filterable $true -Sortable $true)
            (New-SearchField -Name "taggedClients" -Type "Edm.String" -Searchable $true)
            (New-SearchField -Name "isDeleted" -Type "Edm.Boolean" -Filterable $true)
            (New-SearchField -Name "skillChannelName" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "channelOwner" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
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

$indexDefinition = Get-TemplateIndexDefinition -Name $IndexName

Write-Host "Creating/updating index..."
$result = Invoke-SearchRest -Method "PUT" -Url $indexUrl -Body $indexDefinition

Write-Host ""
Write-Host "Index provisioned successfully."
Write-Host "  Name   : $($result.name)"
Write-Host "  Fields : $($result.fields.Count)"
Write-Host ""
Write-Host "Load documents:"
Write-Host "  1. Full crawl  -> POST https://<function-app>/api/TemplateIndexSearch/FullCrawl?code=<key>"
Write-Host "     (Also: /api/ListingIndex/TemplateListing/FullCrawl)"
Write-Host "  2. Single doc  -> POST https://<function-app>/api/TemplateIndexSearch/Upsert?clonableSkillChannelId=<id>&code=<key>"
Write-Host "     (Also: /api/ListingIndex/TemplateListing/Upsert?documentId=<id>&code=<key>)"
Write-Host "  3. Portal      -> Create, edit, delete, or restore a template (auto upsert on save)"
Write-Host ""
Write-Host "App settings required:"
Write-Host "  AzureSearch:Endpoint, AzureSearch:ApiKey"
Write-Host "  AzureSearch:TemplateListing:Enabled=true"
Write-Host "  AzureSearch:TemplateListing:IndexName=$IndexName"
Write-Host "  AzureSearch:TemplateListing:ManageIndexSchema=false"
