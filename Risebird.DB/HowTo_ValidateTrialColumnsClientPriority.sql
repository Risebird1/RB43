/* =============================================================================
   Trial columns QA checklist (trial-columns-clientpriority)
   Staging + product: capture screenshots for PR before production.
   ============================================================================= */

-- Owner P0 strip source (active tab)
EXEC dbo.usp_GetTrialFlagOwnerSummary @ReviewStatus = 'pending';

-- Light list page (Channel Owner + Severity for Client Priority mapping)
IF OBJECT_ID('tempdb..#TfrList') IS NOT NULL DROP TABLE #TfrList;
CREATE TABLE #TfrList (
    TrialFlagId INT,
    InterviewerId INT,
    IrfId INT,
    ChannelId INT,
    TriggeringInterviewId INT,
    TriggeringInterviewDate DATETIME NULL,
    OverallVerdict VARCHAR(30) NULL,
    DisplayVerdict VARCHAR(30) NULL,
    Severity VARCHAR(4) NULL,
    ReviewStatus VARCHAR(20) NULL,
    FlaggedAtUtc DATETIME NULL,
    ReviewedAtUtc DATETIME NULL,
    ReviewedBy VARCHAR(150) NULL,
    ReviewerComment NVARCHAR(500) NULL,
    InterviewerName NVARCHAR(200) NULL,
    InterviewerEmail VARCHAR(200) NULL,
    InterviewerCodedId VARCHAR(50) NULL,
    InterviewerUserId INT NULL,
    InterviewerMobileNumber VARCHAR(20) NULL,
    InterviewerAltMobileNumber NVARCHAR(200) NULL,
    IsSuspicious BIT NULL,
    IrfName NVARCHAR(200) NULL,
    ChannelOwnerEmail VARCHAR(200) NULL,
    ChannelOwner NVARCHAR(300) NULL,
    RiskCategoryLabels VARCHAR(MAX) NULL,
    RiskCategoryReasons VARCHAR(500) NULL,
    QcMergeVerdict VARCHAR(20) NULL,
    QcMergeTotalPenalty INT NULL,
    QcMergeQcScore INT NULL,
    QcMergeScore INT NULL,
    TotalRows INT NULL
);

INSERT INTO #TfrList
EXEC dbo.usp_GetTrialFlagsForReview_List
    @ReviewStatus = 'pending',
    @Offset = 0,
    @PageSize = 25,
    @Search = NULL,
    @Severity = NULL,
    @ChannelOwner = NULL;

SELECT TOP 25
    TrialFlagId,
    ChannelOwner,
    ChannelOwnerEmail,
    Severity,
    QcMergeVerdict,
    IrfName
FROM #TfrList
ORDER BY
    CASE Severity WHEN 'P0' THEN 0 WHEN 'P1' THEN 1 WHEN 'P2' THEN 2
                  WHEN 'P3' THEN 3 WHEN 'P4' THEN 4 WHEN 'P5' THEN 5 ELSE 9 END,
    FlaggedAtUtc DESC;

DROP TABLE #TfrList;

/*
Manual UI checklist
[ ] Channel Owner column shows Name (email)
[ ] Owner dropdown filters grid
[ ] Strip shows "N P0 · M total" for active tab; filter → strip shows that owner only
[ ] No TRANSCRIPT/REPORT/RATING/JOIN/DURATION columns on main grid
[ ] Expand row: raw metrics + rating ⓘ + Severity breakdown
[ ] Client Priority badge Critical/Medium/Low/Clear with · P#
[ ] Sort still P0-first across pages
[ ] At 1366px width: no horizontal scroll on grid
[ ] Screenshots: grid before/after, owner filter+strip, one expanded row
*/
GO
