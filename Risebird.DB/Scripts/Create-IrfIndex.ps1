<#
.SYNOPSIS
    Creates or updates the Azure AI Search IRF listing index (irfindex).

.DESCRIPTION
    Provisions the index schema used by:
      - Risebird.Repository/Indexing/IrfIndexFields.cs      (field name constants)
      - Risebird.Repository/Indexing/IrfIndexDefinition.cs  (C# schema mirror)
      - Risebird.Repository/Indexing/IrfIndexDocumentMapper.cs
      - Risebird.Entities/Search/IrfIndexDocument.cs
      - usp_GetIrfIndexBatch (SQL source)

    UI field mapping
    ----------------
    Filter panel / list card          Index field           Search  Filter
    --------------------------        -----------           ------  ------
    Skill Channel                     irfId                 yes     yes
    Primary Skill                     primarySkill          yes     yes
    Project Name (client company)     clientCompName        yes     yes
    Client Name (end client)          clientName            yes     yes
    Start Date                        startDate             -       yes
    End Date                          endDate               -       yes
    Card title (JD)                   jdTiltle              yes     yes
    Channel Owner                     risebirdHrEmail       yes     yes
    Total Candidates                  totalMeetings         -       yes
    Completed Interviews              scheduledMeetings     -       yes
    Selected Candidates               selectedMeetings      -       yes

    After creating the index, load data via:
      Full crawl : POST /api/IrfIndexSearch/FullCrawl?code=<key>
      Single doc : POST /api/IrfIndexSearch/Upsert?irfIntId=123&code=<key>
    Or create/edit IRF in the portal (automatic upsert).

.PARAMETER SearchServiceName
    Azure AI Search service name (without .search.windows.net).

.PARAMETER SearchEndpoint
    Full endpoint URL. Used when SearchServiceName is not supplied.

.PARAMETER AdminApiKey
    Admin API key for the search service.

.PARAMETER IndexName
    Index name. Default: irfindex

.PARAMETER ApiVersion
    Azure Search REST API version.

.PARAMETER Recreate
    Deletes the existing index before creating it. All documents are removed.

.EXAMPLE
    .\Create-IrfIndex.ps1 `
        -SearchServiceName "risebird-search" `
        -AdminApiKey "<admin-key>"

.EXAMPLE
    .\Create-IrfIndex.ps1 `
        -SearchEndpoint "https://risebird-search.search.windows.net" `
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
    [string] $IndexName = "irfindex",

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

# Must match Risebird.Repository.Indexing.IrfIndexDefinition field flags exactly.
function Get-IrfIndexDefinition {
    param([string] $Name)

    return [ordered]@{
        name   = $Name
        fields = @(
            (New-SearchField -Name "irfIntId" -Type "Edm.String" -Key $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "irfId" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "jdTiltle" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "primarySkill" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "clientCompName" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "clientName" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "risebirdHrEmail" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "startDate" -Type "Edm.DateTimeOffset" -Filterable $true -Sortable $true)
            (New-SearchField -Name "endDate" -Type "Edm.DateTimeOffset" -Filterable $true -Sortable $true)
            (New-SearchField -Name "createdOn" -Type "Edm.DateTimeOffset" -Filterable $true -Sortable $true)
            (New-SearchField -Name "totalMeetings" -Type "Edm.Int32" -Filterable $true -Sortable $true)
            (New-SearchField -Name "scheduledMeetings" -Type "Edm.Int32" -Filterable $true -Sortable $true)
            (New-SearchField -Name "selectedMeetings" -Type "Edm.Int32" -Filterable $true -Sortable $true)
            (New-SearchField -Name "confirmedMeetings" -Type "Edm.Int32" -Filterable $true -Sortable $true)
            (New-SearchField -Name "flagCount" -Type "Edm.Int32" -Filterable $true -Sortable $true)
            (New-SearchField -Name "isDeleted" -Type "Edm.Boolean" -Filterable $true)
            (New-SearchField -Name "isPaused" -Type "Edm.Boolean" -Filterable $true)
            (New-SearchField -Name "allowAllInternal" -Type "Edm.Boolean" -Filterable $true)
            (New-SearchField -Name "curatedLink" -Type "Edm.Boolean" -Filterable $true)
            (New-SearchField -Name "allowedViewerEmails" -Type "Collection(Edm.String)" -Filterable $true)
            (New-SearchField -Name "jdLink" -Type "Edm.String" -Searchable $true)
            (New-SearchField -Name "risebirdHr" -Type "Edm.Int32" -Filterable $true)
            (New-SearchField -Name "perMintuteRate" -Type "Edm.Double" -Filterable $true -Sortable $true)
            (New-SearchField -Name "isClientUser" -Type "Edm.Boolean" -Filterable $true)
            (New-SearchField -Name "totalInvites" -Type "Edm.Int32" -Filterable $true -Sortable $true)
            (New-SearchField -Name "totalBudget" -Type "Edm.Int32" -Filterable $true -Sortable $true)
            (New-SearchField -Name "totalBudgetConsumed" -Type "Edm.Double" -Filterable $true -Sortable $true)
            (New-SearchField -Name "calendlyLink" -Type "Edm.String" -Searchable $true)
            (New-SearchField -Name "companyDetails" -Type "Edm.String" -Searchable $true)
            (New-SearchField -Name "discount" -Type "Edm.Double" -Filterable $true)
            (New-SearchField -Name "isBiometricEnabled" -Type "Edm.Boolean" -Filterable $true)
            (New-SearchField -Name "priorityLevel" -Type "Edm.Int32" -Filterable $true -Sortable $true)
            (New-SearchField -Name "createdFromTemplateId" -Type "Edm.Int32" -Filterable $true)
            (New-SearchField -Name "assessmentGroupId" -Type "Edm.String" -Searchable $true -Filterable $true)
            (New-SearchField -Name "isAIInterview" -Type "Edm.Boolean" -Filterable $true)
            (New-SearchField -Name "aiInterviewType" -Type "Edm.Int32" -Filterable $true)
            (New-SearchField -Name "interviewerBillingCycle" -Type "Edm.Int32" -Filterable $true)
            (New-SearchField -Name "affindaEmbedUrl" -Type "Edm.String" -Searchable $true)
            (New-SearchField -Name "affindaEmbedUrlCreatedAt" -Type "Edm.DateTimeOffset" -Filterable $true -Sortable $true)
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

$indexDefinition = Get-IrfIndexDefinition -Name $IndexName

Write-Host "Creating/updating index..."
$result = Invoke-SearchRest -Method "PUT" -Url $indexUrl -Body $indexDefinition

Write-Host ""
Write-Host "Index provisioned successfully."
Write-Host "  Name   : $($result.name)"
Write-Host "  Fields : $($result.fields.Count)"
Write-Host ""
Write-Host "Load documents:"
Write-Host "  1. Full crawl  -> POST https://<function-app>/api/IrfIndexSearch/FullCrawl?code=<key>"
Write-Host "  2. Single IRF  -> POST https://<function-app>/api/IrfIndexSearch/Upsert?irfIntId=<id>&code=<key>"
Write-Host "  3. Portal      -> Create or Edit IRF (auto upsert on save)"
Write-Host ""
Write-Host "App settings required:"
Write-Host "  AzureSearch:Endpoint, AzureSearch:ApiKey"
Write-Host "  AzureSearch:IrfListing:Enabled=true"
Write-Host "  AzureSearch:IrfListing:IndexName=$IndexName"
Write-Host "  AzureSearch:IrfListing:ManageIndexSchema=false"
