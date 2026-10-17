/*
================================================================================
usp_GetQCActivityCoverage — QC Activity high-risk coverage (part 2).

High-Risk = the High-Risk Interviewers tab's list for the window
(usp_QCActivity_HighRisk). Touched = any call, coaching session or action about
the interviewer by any QC person in the window.

Result sets:
  1 Totals     HighRiskTotal, HighRiskTouched, CoveragePct
  2 Untouched  top 50 High-Risk interviewers nobody touched, worst first:
               InterviewerId, CodedId, InterviewerName, InterviewerEmail,
               OverallRiskRatio, WorstSeverity, PriorityTier, TotalCompleted, TotalBad

usp_GetQCActivityDashboardSummary returns the same two sets as its result sets
3 and 4.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetQCActivityCoverage]
    @FromIst DATE,
    @ToIst   DATE
AS
BEGIN
    SET NOCOUNT ON;

    CREATE TABLE #QCActivityRows (
        ActorEmail        VARCHAR(150)  NOT NULL,
        Metric            VARCHAR(30)   NOT NULL,
        ActionAtIst       DATETIME      NULL,
        InterviewId       INT           NULL,
        IrfId             INT           NULL,
        InterviewerUserId INT           NULL,
        InterviewerEmail  VARCHAR(200)  NULL,
        Amount            INT           NULL,
        Detail            NVARCHAR(500) NULL,
        [Status]          VARCHAR(150)  NULL,
        Phone             VARCHAR(30)   NULL,
        RecordingUrl      VARCHAR(1000) NULL,
        SourceId          INT           NULL,
        RefId             INT           NULL,
        Comment           NVARCHAR(500) NULL,
        HasEvidence       BIT           NULL,
        IsAuthentic       BIT           NULL,
        PageGroup         VARCHAR(30)   NULL
    );

    EXEC dbo.usp_QCActivity_CollectRows @FromIst = @FromIst, @ToIst = @ToIst, @ActorEmail = NULL, @Metric = NULL;

    CREATE TABLE #QCHighRisk (
        InterviewerId    INT           NOT NULL PRIMARY KEY,
        CodedId          NVARCHAR(200) NULL,
        InterviewerName  NVARCHAR(310) NULL,
        InterviewerEmail VARCHAR(200)  NULL,
        OverallRiskRatio DECIMAL(10,4) NULL,
        WorstSeverity    VARCHAR(4)    NULL,
        PriorityTier     VARCHAR(20)   NULL,
        TotalCompleted   INT           NULL,
        TotalBad         INT           NULL,
        Touched          BIT           NOT NULL DEFAULT 0
    );

    EXEC dbo.usp_QCActivity_HighRisk @FromIst = @FromIst, @ToIst = @ToIst;

    SELECT
        (SELECT COUNT(1) FROM #QCHighRisk) AS HighRiskTotal,
        (SELECT COUNT(1) FROM #QCHighRisk WHERE Touched = 1) AS HighRiskTouched,
        CAST(CASE WHEN (SELECT COUNT(1) FROM #QCHighRisk) = 0 THEN NULL
                  ELSE 100.0 * (SELECT COUNT(1) FROM #QCHighRisk WHERE Touched = 1) / (SELECT COUNT(1) FROM #QCHighRisk) END
             AS DECIMAL(6,1)) AS CoveragePct;

    SELECT TOP (50)
        h.InterviewerId, h.CodedId, h.InterviewerName, h.InterviewerEmail, h.OverallRiskRatio,
        h.WorstSeverity, h.PriorityTier, h.TotalCompleted, h.TotalBad
    FROM #QCHighRisk h
    WHERE h.Touched = 0
    ORDER BY CASE h.WorstSeverity WHEN 'P0' THEN 0 WHEN 'P1' THEN 1 WHEN 'P2' THEN 2 WHEN 'P3' THEN 3
                                  WHEN 'P4' THEN 4 WHEN 'P5' THEN 5 ELSE 9 END,
             ISNULL(h.OverallRiskRatio, 0) DESC, h.InterviewerId;

    DROP TABLE IF EXISTS #QCHighRisk;
    DROP TABLE IF EXISTS #QCActivityRows;
END
GO
