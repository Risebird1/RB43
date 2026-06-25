CREATE OR ALTER PROCEDURE [dbo].[usp_RecordAutoQcBackfillRun]

    @BatchId UNIQUEIDENTIFIER,

    @RecordsRemoved INT,

    @RecordsSkipped INT,

    @TriggeringInterviewCount INT,

    @ExecutedBy VARCHAR(150)

AS

BEGIN

    SET NOCOUNT ON;



    INSERT INTO dbo.AutoQcBackfillRun

        (BatchId, ExecutedAtUtc, RecordsRemoved, RecordsSkipped, TriggeringInterviewCount, ExecutedBy)

    VALUES

        (@BatchId, GETUTCDATE(), @RecordsRemoved, @RecordsSkipped, @TriggeringInterviewCount, @ExecutedBy);

END

GO

