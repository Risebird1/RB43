CREATE OR ALTER PROCEDURE [dbo].[usp_GetRiskCategoryBackfillCandidatesByIrf]
    @IrfId   INT,
    @FromIst DATE = NULL,
    @ToIst   DATE = NULL,
    @MaxRows INT = 100
AS
/*
  Returns interview IDs for RiskCategory backfill within the IRF date window.
  Prefer-missing first (empty / NULL Severity), then existing rows so corrective
  re-grades (e.g. after a check predicate fix) can rewrite SeverityBreakdown.
*/
BEGIN
    SET NOCOUNT ON;

    IF @IrfId IS NULL OR @IrfId <= 0
    BEGIN
        SELECT CAST(0 AS INT) AS InterviewId WHERE 1 = 0;
        RETURN;
    END

    IF @ToIst IS NULL
        SET @ToIst = CAST(DATEADD(MINUTE, 330, GETUTCDATE()) AS DATE);
    IF @FromIst IS NULL
        SET @FromIst = DATEADD(DAY, -30, @ToIst);
    IF @FromIst > @ToIst
        SET @FromIst = DATEADD(DAY, -30, @ToIst);
    IF DATEDIFF(DAY, @FromIst, @ToIst) > 90
        SET @FromIst = DATEADD(DAY, -90, @ToIst);

    IF @MaxRows IS NULL OR @MaxRows <= 0
        SET @MaxRows = 100;
    IF @MaxRows > 1000
        SET @MaxRows = 1000;

    SELECT TOP (@MaxRows)
        im.id AS InterviewId
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
    LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = im.id
    WHERE irf.id = @IrfId
      AND ic.MeetingStatus IN (4, 6, 7, 8)
      AND CAST(DATEADD(MINUTE, 330, im.interviewDate) AS DATE) >= @FromIst
      AND CAST(DATEADD(MINUTE, 330, im.interviewDate) AS DATE) <= @ToIst
    ORDER BY
        CASE
            WHEN rc.InterviewId IS NULL
              OR NULLIF(LTRIM(RTRIM(ISNULL(rc.Severity, ''))), '') IS NULL
            THEN 0 ELSE 1
        END,
        im.interviewDate DESC,
        im.id DESC;
END
GO
