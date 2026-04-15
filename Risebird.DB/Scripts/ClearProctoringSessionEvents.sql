/*
  Run manually on the target database before go-live with the new meeting-window rules.
  WARNING: Deletes all rows in dbo.ProctoringSessionEvents.
*/
SET NOCOUNT ON;
GO
DELETE FROM dbo.ProctoringSessionEvents;
GO
