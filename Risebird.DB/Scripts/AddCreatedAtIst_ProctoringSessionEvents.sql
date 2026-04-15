-- Adds IST wall-clock time for proctoring events (India Standard Time).
-- Run on your Risebird database before deploying the updated API.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.ProctoringSessionEvents') AND name = N'CreatedAtIst'
)
BEGIN
    ALTER TABLE [dbo].[ProctoringSessionEvents] ADD [CreatedAtIst] DATETIME2(7) NULL;
END
GO
