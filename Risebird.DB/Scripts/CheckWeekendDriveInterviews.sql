-- Weekend Drive: check confirmed interviews for the upcoming Sat–Sun window.
-- Run on the same DB your app uses. Adjust @today if needed.

DECLARE @today date = CAST(GETDATE() AS date);

-- "This weekend" = upcoming Saturday through Sunday (same logic as InterviewDemandController)
DECLARE @thisSat date =
    CASE DATEPART(weekday, @today)
        WHEN 1 THEN DATEADD(day, -1, @today)  -- Sunday: Sat just passed
        ELSE DATEADD(day, (7 - DATEPART(weekday, @today)) % 7, @today)
    END;
DECLARE @thisSun date = DATEADD(day, 1, @thisSat);

DECLARE @nextSat date = DATEADD(day, 7, @thisSat);
DECLARE @nextSun date = DATEADD(day, 1, @nextSat);

SELECT 'This weekend' AS [Window], @thisSat AS StartDate, @thisSun AS EndDate
UNION ALL
SELECT 'Next weekend', @nextSat, @nextSun;

-- Confirmed interviews in THIS weekend (confirmationStatus = 1)
SELECT
    im.IRFId,
    CAST(im.interviewDate AS date) AS InterviewDate,
    im.interviewSlot,
    im.interviewer,
    COUNT(*) AS InterviewCount
FROM dbo.InterviewMeetings im
WHERE CAST(im.interviewDate AS date) BETWEEN @thisSat AND @thisSun
  AND im.confirmationStatus = 1
GROUP BY im.IRFId, CAST(im.interviewDate AS date), im.interviewSlot, im.interviewer
ORDER BY InterviewDate, im.IRFId;

-- Per-IRF totals for THIS weekend (drive threshold default = 5)
SELECT
    im.IRFId,
    COUNT(*) AS TotalConfirmed
FROM dbo.InterviewMeetings im
WHERE CAST(im.interviewDate AS date) BETWEEN @thisSat AND @thisSun
  AND im.confirmationStatus = 1
GROUP BY im.IRFId
HAVING COUNT(*) >= 5
ORDER BY TotalConfirmed DESC;
