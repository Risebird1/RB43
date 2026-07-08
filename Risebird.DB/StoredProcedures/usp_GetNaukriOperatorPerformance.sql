IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetNaukriOperatorPerformance')
    DROP PROCEDURE usp_GetNaukriOperatorPerformance
GO

-- Operator performance for the NaukriOnboarding "who did what between given dates" supervision page.
-- Everything is derived from the NaukriOnboardingHistory audit log.
--
-- @DateFrom / @DateTo are IST calendar dates (inclusive). ChangedAt is stored in UTC, so the window
-- is converted to UTC (IST = UTC + 5:30) before filtering.
-- @Operator optionally restricts to a single ChangedBy (email).
-- @IncludeSystem = 0 hides automation / webhook actors ('System', 'AutomationJob', 'PlivoCX Webhook',
-- 'Relay Webhook') so the focus is on human operators.
--
-- Returns THREE result sets:
--   1) Per-operator summary   (Operator, TotalActions, TicketsTouched, FirstActionAt, LastActionAt)
--   2) Per-operator x event   (Operator, EventType, Cnt)  -> matrix built in the app layer
--   3) Detailed activity log  (ChangedAt, Operator, RecordId, CandidateName, MagName, EventType, FromValue, ToValue)
CREATE PROCEDURE [dbo].[usp_GetNaukriOperatorPerformance]
    @DateFrom      DATE,
    @DateTo        DATE,
    @Operator      VARCHAR(150) = NULL,
    @IncludeSystem BIT          = 0,
    @LogLimit      INT          = 1000
AS
BEGIN
    SET NOCOUNT ON;

    IF @DateFrom IS NULL SET @DateFrom = CAST(DATEADD(DAY, -30, GETDATE()) AS DATE);
    IF @DateTo   IS NULL SET @DateTo   = CAST(GETDATE() AS DATE);
    IF @LogLimit IS NULL OR @LogLimit <= 0 SET @LogLimit = 1000;

    DECLARE @FromUtc DATETIME = DATEADD(MINUTE, -330, CAST(@DateFrom AS DATETIME));
    DECLARE @ToUtc   DATETIME = DATEADD(MINUTE, -330, DATEADD(DAY, 1, CAST(@DateTo AS DATETIME)));

    -- Materialize the filtered window once (a CTE would only be visible to the first of the three
    -- result-set SELECTs below, so a temp table is required here).
    IF OBJECT_ID('tempdb..#H') IS NOT NULL DROP TABLE #H;
    SELECT h.RecordId, h.EventType, h.FromValue, h.ToValue, h.ChangedBy, h.ChangedAt
    INTO #H
    FROM dbo.NaukriOnboardingHistory h
    WHERE h.ChangedAt >= @FromUtc
      AND h.ChangedAt <  @ToUtc
      AND (@Operator IS NULL OR h.ChangedBy = @Operator)
      AND (@IncludeSystem = 1 OR ISNULL(h.ChangedBy, 'System') NOT IN ('System', 'AutomationJob', 'PlivoCX Webhook', 'Relay Webhook'));

    -- 1) Summary
    SELECT
        ISNULL(h.ChangedBy, 'System')          AS Operator,
        COUNT(*)                               AS TotalActions,
        COUNT(DISTINCT h.RecordId)             AS TicketsTouched,
        DATEADD(MINUTE, 330, MIN(h.ChangedAt)) AS FirstActionAt,
        DATEADD(MINUTE, 330, MAX(h.ChangedAt)) AS LastActionAt
    FROM #H h
    GROUP BY ISNULL(h.ChangedBy, 'System')
    ORDER BY TotalActions DESC;

    -- 2) Per-operator x event-type counts (matrix source)
    SELECT
        ISNULL(h.ChangedBy, 'System') AS Operator,
        h.EventType                   AS EventType,
        COUNT(*)                      AS Cnt
    FROM #H h
    GROUP BY ISNULL(h.ChangedBy, 'System'), h.EventType;

    -- 3) Detailed activity log (most recent first, capped)
    SELECT TOP (@LogLimit)
        DATEADD(MINUTE, 330, h.ChangedAt) AS ChangedAt,
        ISNULL(h.ChangedBy, 'System')     AS Operator,
        h.RecordId                        AS RecordId,
        np.CandidateName                  AS CandidateName,
        np.MagName                        AS MagName,
        h.EventType                       AS EventType,
        h.FromValue                       AS FromValue,
        h.ToValue                         AS ToValue
    FROM #H h
    LEFT JOIN dbo.NaukriProspectiveInterviewer np ON np.Id = h.RecordId
    ORDER BY h.ChangedAt DESC;

    IF OBJECT_ID('tempdb..#H') IS NOT NULL DROP TABLE #H;
END
GO
