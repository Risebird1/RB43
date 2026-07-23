/* =============================================================================
   Trial Flag Review — List vs Detail parity smoke (post-deploy)

   Checks:
     1) Sample P0-first TrialFlagIds appear on List (first 200, all statuses)
     2) List identity/light fields match Detail for the same TrialFlagId
     3) List proc definition has no JSON_VALUE / PaymentSLADetails (expect 0 rows)

   Adjust #List CREATE TABLE if List result shape drifts.
   ============================================================================= */

DECLARE @Sample TABLE (TrialFlagId INT PRIMARY KEY);

INSERT INTO @Sample (TrialFlagId)
SELECT TOP (20) tf.Id
FROM dbo.TrialFlags tf
ORDER BY
    CASE WHEN tf.Severity = 'P0' THEN 0
         WHEN tf.Severity = 'P1' THEN 1
         WHEN tf.Severity = 'P2' THEN 2
         WHEN tf.Severity = 'P3' THEN 3
         WHEN tf.Severity = 'P4' THEN 4
         WHEN tf.Severity = 'P5' THEN 5
         ELSE 9 END,
    tf.Id DESC;

IF OBJECT_ID('tempdb..#List') IS NOT NULL DROP TABLE #List;
CREATE TABLE #List (
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

INSERT INTO #List
EXEC dbo.usp_GetTrialFlagsForReview_List
    @ReviewStatus = NULL,
    @Offset = 0,
    @PageSize = 200,
    @Search = NULL,
    @Severity = NULL,
    @ChannelOwner = NULL;

-- Presence + light columns from List
SELECT
    s.TrialFlagId,
    CASE WHEN L.TrialFlagId IS NULL THEN N'MISSING_FROM_LIST_PAGE' ELSE N'OK' END AS ListPresence,
    L.InterviewerId,
    L.IrfId,
    L.TriggeringInterviewId,
    L.Severity,
    L.OverallVerdict,
    L.DisplayVerdict,
    L.ReviewStatus,
    L.QcMergeVerdict,
    L.ChannelOwner
FROM @Sample s
LEFT JOIN #List L ON L.TrialFlagId = s.TrialFlagId
ORDER BY s.TrialFlagId;

-- Per-sample Detail smoke (run in SSMS Results to Text / multiple grids)
DECLARE @Id INT;
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT TrialFlagId FROM @Sample;
OPEN c;
FETCH NEXT FROM c INTO @Id;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @Id AS CheckingTrialFlagId;
    EXEC dbo.usp_GetTrialFlagDetail @TrialFlagId = @Id;
    FETCH NEXT FROM c INTO @Id;
END
CLOSE c;
DEALLOCATE c;

-- Guardrail: List must not reference JSON_VALUE or PaymentSLADetails
SELECT N'LIST_DEF_HAS_HEAVY_PROBE' AS Finding
WHERE OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_GetTrialFlagsForReview_List')) LIKE N'%JSON_VALUE%'
   OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_GetTrialFlagsForReview_List')) LIKE N'%PaymentSLADetails%';
-- Expect: 0 rows

DROP TABLE #List;
GO
