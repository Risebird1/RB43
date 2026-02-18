IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetQCOwnerDashboardMetrics'))
BEGIN
    DROP PROCEDURE dbo.usp_GetQCOwnerDashboardMetrics
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Founder QC Owner Dashboard: metrics per QC owner (CreatedBy) for completed QC reports in date range.
-- Maps to PaymentSLADetails (QC data); joins InterviewMeetings (Redo), ProxyReport (proxy), InterviewComments (QCStatus).
CREATE PROCEDURE dbo.usp_GetQCOwnerDashboardMetrics
    @DateFrom DATE = NULL,
    @DateTo   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @DateFrom IS NULL
        SET @DateFrom = DATEADD(DAY, -30, CAST(GETDATE() AS DATE));
    IF @DateTo IS NULL
        SET @DateTo = CAST(GETDATE() AS DATE);

    ;WITH Base AS (
        SELECT
            psd.CreatedBy,
            psd.Interviewid,
            psd.CreatedOn,
            psd.PaymentDeduction,
            psd.PaymentComplianceCount,
            psd.Recommendation,
            im.RedoInterview,
            pr.isProxyCandidate AS ProxyReported,
            ic.QCStatus,

            -- Flag count: each flag column non-empty = 1 (PaymentSLADetails stores varchar)
            (CASE WHEN LEN(LTRIM(RTRIM(ISNULL(psd.Inaccuratedecisionmaking,'')))) > 0 THEN 1 ELSE 0 END) +
            (CASE WHEN LEN(LTRIM(RTRIM(ISNULL(psd.TimeManagement,'')))) > 0 THEN 1 ELSE 0 END) +
            (CASE WHEN LEN(LTRIM(RTRIM(ISNULL(psd.QuestionsRelevance,'')))) > 0 THEN 1 ELSE 0 END) +
            (CASE WHEN LEN(LTRIM(RTRIM(ISNULL(psd.ScoreRubricIgnorance,'')))) > 0 THEN 1 ELSE 0 END) +
            (CASE WHEN LEN(LTRIM(RTRIM(ISNULL(psd.PoorFeedbackComments,'')))) > 0 THEN 1 ELSE 0 END) +
            (CASE WHEN LEN(LTRIM(RTRIM(ISNULL(psd.CandidateInteractionQuality,'')))) > 0 THEN 1 ELSE 0 END) +
            (CASE WHEN LEN(LTRIM(RTRIM(ISNULL(psd.LimitedHandoncoding,'')))) > 0 THEN 1 ELSE 0 END) +
            (CASE WHEN LEN(LTRIM(RTRIM(ISNULL(psd.WrongStatusUpdates,'')))) > 0 THEN 1 ELSE 0 END) +
            (CASE WHEN LEN(LTRIM(RTRIM(ISNULL(psd.WeakInfrastructure,'')))) > 0 THEN 1 ELSE 0 END) +
            (CASE WHEN LEN(LTRIM(RTRIM(ISNULL(psd.ProctoringIgnorance,'')))) > 0 THEN 1 ELSE 0 END) +
            (CASE WHEN LEN(LTRIM(RTRIM(ISNULL(psd.UnethicalPractices,'')))) > 0 THEN 1 ELSE 0 END) +
            (CASE WHEN LEN(LTRIM(RTRIM(ISNULL(psd.PoorCommunicationSkills,'')))) > 0 THEN 1 ELSE 0 END)
            AS FlagCount
        FROM PaymentSLADetails psd
        INNER JOIN InterviewMeetings im ON im.id = psd.Interviewid
        LEFT JOIN InterviewComments ic ON ic.InterviewId = psd.Interviewid
        LEFT JOIN ProxyReport pr ON pr.interviewid = psd.Interviewid AND ISNULL(pr.isProxyCandidate, 0) = 1
        WHERE psd.isQcDone = 1
          AND psd.CreatedOn >= @DateFrom
          AND psd.CreatedOn < DATEADD(DAY, 1, @DateTo)
    )
    SELECT
        CreatedBy AS QCOwner,

        -- 1) QC Done
        COUNT(*) AS QCDone,

        -- 2) Reports With Flags
        SUM(CASE WHEN FlagCount >= 1 THEN 1 ELSE 0 END) AS ReportsWithFlags,

        -- 3) Reports Without Flags
        SUM(CASE WHEN FlagCount = 0 THEN 1 ELSE 0 END) AS ReportsWithoutFlags,

        -- 4) Critical Queue – No Flags (Under Review / High Severity Pending / Fake – with zero flags)
        -- Under Review: QCStatus NULL or pending-like; High Severity: PaymentComplianceCount > 0; Fake: not in schema, omitted
        SUM(
            CASE
                WHEN (
                    (QCStatus IS NULL OR QCStatus = 0)
                    OR (ISNULL(PaymentComplianceCount, 0) > 0)
                )
                AND FlagCount = 0
                THEN 1
                ELSE 0
            END
        ) AS CriticalQueue_NoFlags,

        -- 5) Total Flags Reported (sum of individual flags)
        SUM(FlagCount) AS TotalFlagsReported,

        -- 6) Total Redo Marked (InterviewMeetings.RedoInterview or Recommendation = 'REDO')
        SUM(CASE WHEN ISNULL(RedoInterview, 0) = 1 OR UPPER(LTRIM(RTRIM(ISNULL(Recommendation,'')))) = 'REDO' THEN 1 ELSE 0 END) AS TotalRedoMarked,

        -- 7) Total Proxy Marked
        SUM(CASE WHEN ISNULL(ProxyReported, 0) = 1 THEN 1 ELSE 0 END) AS TotalProxyMarked,

        -- 8) Total Penalty (PaymentDeduction as % or value; Penalized count)
        SUM(ISNULL(PaymentDeduction, 0)) AS TotalPenaltyPercent,
        SUM(
            CASE
                WHEN ISNULL(PaymentDeduction, 0) > 0 OR ISNULL(PaymentComplianceCount, 0) > 0
                THEN 1
                ELSE 0
            END
        ) AS PenalizedReports
    FROM Base
    GROUP BY CreatedBy
    ORDER BY QCDone DESC;
END
GO
