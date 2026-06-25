CREATE OR ALTER PROCEDURE [dbo].[usp_GetAutoQcBackfillRunSummary]

AS

BEGIN

    SET NOCOUNT ON;



    IF OBJECT_ID(N'dbo.AutoQcBackfillRun', N'U') IS NULL

        RETURN;



    SELECT TOP 1

        BatchId,

        ExecutedAtUtc,

        RecordsRemoved,

        TriggeringInterviewCount,

        ExecutedBy

    FROM dbo.AutoQcBackfillRun

    ORDER BY ExecutedAtUtc DESC, Id DESC;

END

GO

