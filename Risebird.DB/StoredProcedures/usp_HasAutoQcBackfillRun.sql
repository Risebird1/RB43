CREATE OR ALTER PROCEDURE [dbo].[usp_HasAutoQcBackfillRun]

AS

BEGIN

    SET NOCOUNT ON;



    IF OBJECT_ID(N'dbo.AutoQcBackfillRun', N'U') IS NULL

    BEGIN

        SELECT CAST(0 AS BIT) AS HasRun, CAST(NULL AS UNIQUEIDENTIFIER) AS LatestBatchId, CAST(NULL AS DATETIME) AS LatestExecutedAtUtc;

        RETURN;

    END



    SELECT TOP 1

        CAST(1 AS BIT) AS HasRun,

        BatchId AS LatestBatchId,

        ExecutedAtUtc AS LatestExecutedAtUtc

    FROM dbo.AutoQcBackfillRun

    ORDER BY ExecutedAtUtc DESC, Id DESC;

END

GO

