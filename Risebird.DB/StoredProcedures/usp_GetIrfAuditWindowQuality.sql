/*
================================================================================
usp_GetIrfAuditWindowQuality — one-row window quality strip for the IRF Audit
drill-down: TotalCompleted, BadInterviews, BadRatio, P0..P5 counts for
completed interviews in the same IST window used by
usp_GetIrfAuditInterviewerRiskBreakdown. Bad = has a RiskCategory row with
Severity P0-P5. Does not touch recommendation thresholds or grading logic.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetIrfAuditWindowQuality]
    @IrfId   INT,
    @FromIst DATE = NULL,
    @ToIst   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @IrfId IS NULL OR @IrfId <= 0
    BEGIN
        SELECT
            CAST(0 AS INT) AS TotalCompleted,
            CAST(0 AS INT) AS BadInterviews,
            CAST(NULL AS DECIMAL(10,4)) AS BadRatio,
            CAST(0 AS INT) AS P0Count, CAST(0 AS INT) AS P1Count, CAST(0 AS INT) AS P2Count,
            CAST(0 AS INT) AS P3Count, CAST(0 AS INT) AS P4Count, CAST(0 AS INT) AS P5Count;
        RETURN;
    END

    -- IST window (same normalization as usp_GetIrfAuditDirectory / usp_GetIrfAuditInterviewerRiskBreakdown)
    IF @ToIst IS NULL
        SET @ToIst = CAST(DATEADD(MINUTE, 330, GETUTCDATE()) AS DATE);
    IF @FromIst IS NULL
        SET @FromIst = DATEADD(DAY, -30, @ToIst);
    IF @FromIst > @ToIst
        SET @FromIst = DATEADD(DAY, -30, @ToIst);
    IF DATEDIFF(DAY, @FromIst, @ToIst) > 90
        SET @FromIst = DATEADD(DAY, -90, @ToIst);

    DECLARE @IrfName VARCHAR(200);
    SELECT @IrfName = irf.IRFId FROM dbo.IRF irf WHERE irf.id = @IrfId;

    IF @IrfName IS NULL
    BEGIN
        SELECT
            CAST(0 AS INT) AS TotalCompleted,
            CAST(0 AS INT) AS BadInterviews,
            CAST(NULL AS DECIMAL(10,4)) AS BadRatio,
            CAST(0 AS INT) AS P0Count, CAST(0 AS INT) AS P1Count, CAST(0 AS INT) AS P2Count,
            CAST(0 AS INT) AS P3Count, CAST(0 AS INT) AS P4Count, CAST(0 AS INT) AS P5Count;
        RETURN;
    END

    ;WITH Completed AS (
        SELECT im.id AS InterviewId
        FROM dbo.InterviewMeetings im
        INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
        WHERE im.IRFId = @IrfName
          AND ic.MeetingStatus IN (4, 6, 7, 8)
          AND CAST(DATEADD(MINUTE, 330, im.interviewDate) AS DATE) >= @FromIst
          AND CAST(DATEADD(MINUTE, 330, im.interviewDate) AS DATE) <= @ToIst
    )
    SELECT
        COUNT(DISTINCT c.InterviewId) AS TotalCompleted,
        COUNT(DISTINCT CASE WHEN rc.InterviewId IS NOT NULL AND UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) LIKE 'P[0-5]' THEN c.InterviewId END) AS BadInterviews,
        CAST(CASE WHEN COUNT(DISTINCT c.InterviewId) = 0 THEN NULL
                  ELSE CAST(COUNT(DISTINCT CASE WHEN rc.InterviewId IS NOT NULL AND UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) LIKE 'P[0-5]' THEN c.InterviewId END) AS DECIMAL(18,4))
                       / NULLIF(COUNT(DISTINCT c.InterviewId), 0)
             END AS DECIMAL(10,4)) AS BadRatio,
        COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P0' THEN c.InterviewId END) AS P0Count,
        COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P1' THEN c.InterviewId END) AS P1Count,
        COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P2' THEN c.InterviewId END) AS P2Count,
        COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P3' THEN c.InterviewId END) AS P3Count,
        COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P4' THEN c.InterviewId END) AS P4Count,
        COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P5' THEN c.InterviewId END) AS P5Count
    FROM Completed c
    LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = c.InterviewId;
END
GO
