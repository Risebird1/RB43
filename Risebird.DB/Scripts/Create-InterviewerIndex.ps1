<#
.SYNOPSIS
    Creates or updates the Azure AI Search interviewer roster index (interviewerindex).

.DESCRIPTION
    Provisions the index schema used by:
      - Risebird.Repository/Indexing/InterviewerIndexFields.cs
      - Risebird.Repository/Indexing/InterviewerIndexDefinition.cs
      - Risebird.Repository/Indexing/InterviewerIndexDocumentMapper.cs
      - Risebird.Entities/Search/InterviewerIndexDocument.cs
      - usp_GetInterviewerIndexBatch

    After creating the index, load data via:
      Full crawl : POST /api/ListingIndex/InterviewerListing/FullCrawl?code=<key>
      Single doc : POST /api/ListingIndex/InterviewerListing/Upsert?documentId=<profileId>&code=<key>
    Or add/edit interviewer in the portal (automatic upsert).

.PARAMETER SearchServiceName
    Azure AI Search service name (without .search.windows.net).

.PARAMETER SearchEndpoint
    Full endpoint URL. Used when SearchServiceName is not supplied.

.PARAMETER AdminApiKey
    Admin API key for the search service.

.PARAMETER IndexName
    Index name. Default: interviewerindex

.PARAMETER ApiVersion
    Azure Search REST API version.

.PARAMETER Recreate
    Deletes the existing index before creating it. All documents are removed.

.EXAMPLE
    .\Create-InterviewerIndex.ps1 `
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
    [string] $IndexName = "interviewerindex",

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

function Get-InterviewerIndexDefinition {
    param([string] $Name)

    return [ordered]@{
        name   = $Name
        fields = @(
            (New-SearchField -Name "interviewerProfileId" -Type "Edm.String" -Key $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "userId" -Type "Edm.Int32" -Filterable $true)
            (New-SearchField -Name "firstName" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "lastName" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "nickName" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "emailId" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "mobileNumber" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "primarySkill" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "skills" -Type "Collection(Edm.String)" -Searchable $true -Filterable $true)
            (New-SearchField -Name "assessmentGroupId" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "assessmentGroupNames" -Type "Collection(Edm.String)" -Searchable $true -Filterable $true)
            (New-SearchField -Name "senioritylevel" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "perHourRateIndia" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "perHourRateOutside" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "compentencyLevel" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "profileType" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "companyType" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "country" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true -Facetable $true)
            (New-SearchField -Name "isLeadInterviewer" -Type "Edm.Boolean" -Filterable $true)
            (New-SearchField -Name "isActive" -Type "Edm.Boolean" -Filterable $true)
            (New-SearchField -Name "isInterviewerAccess" -Type "Edm.Boolean" -Filterable $true)
            (New-SearchField -Name "isApproved" -Type "Edm.Boolean" -Filterable $true)
            (New-SearchField -Name "isInterested" -Type "Edm.Boolean" -Filterable $true)
            (New-SearchField -Name "isDeleted" -Type "Edm.Boolean" -Filterable $true)
            (New-SearchField -Name "isavailable" -Type "Edm.Int32" -Filterable $true)
            (New-SearchField -Name "availabilityStatusValue" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "videoConfirmation" -Type "Edm.Int32" -Filterable $true)
            (New-SearchField -Name "resumeLink" -Type "Edm.String" -Searchable $true)
            (New-SearchField -Name "codedResumeLink" -Type "Edm.String" -Searchable $true)
            (New-SearchField -Name "city" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "currentRole" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "experience" -Type "Edm.Double" -Filterable $true -Sortable $true)
            (New-SearchField -Name "preferredDomain" -Type "Edm.String" -Searchable $true -Filterable $true -Sortable $true)
            (New-SearchField -Name "uniqueid" -Type "Edm.String" -Searchable $true)
            (New-SearchField -Name "availabilityResponseDate" -Type "Edm.DateTimeOffset" -Filterable $true -Sortable $true)
            (New-SearchField -Name "isHoldPayment" -Type "Edm.Boolean" -Filterable $true)
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

$indexDefinition = Get-InterviewerIndexDefinition -Name $IndexName

Write-Host "Creating/updating index..."
$result = Invoke-SearchRest -Method "PUT" -Url $indexUrl -Body $indexDefinition

Write-Host ""
Write-Host "Index provisioned successfully."
Write-Host "  Name   : $($result.name)"
Write-Host "  Fields : $($result.fields.Count)"
Write-Host ""
Write-Host "Load documents:"
Write-Host "  1. Full crawl  -> POST https://<function-app>/api/InterviewerIndexSearch/FullCrawl?code=<key>"
Write-Host "  2. Single doc  -> POST https://<function-app>/api/InterviewerIndexSearch/Upsert?interviewerProfileId=<id>&code=<key>"
Write-Host "  3. By user id  -> POST https://<function-app>/api/InterviewerIndexSearch/Upsert?userId=<id>&code=<key>"
Write-Host "  4. Portal      -> Add or Edit Interviewer (auto upsert on save)"
Write-Host "  (Also available: /api/ListingIndex/InterviewerListing/FullCrawl)"
Write-Host ""
Write-Host "App settings required:"
Write-Host "  AzureSearch:Endpoint, AzureSearch:ApiKey"
Write-Host "  AzureSearch:InterviewerListing:Enabled=true"
Write-Host "  AzureSearch:InterviewerListing:IndexName=$IndexName"
Write-Host "  AzureSearch:InterviewerListing:ManageIndexSchema=false"
