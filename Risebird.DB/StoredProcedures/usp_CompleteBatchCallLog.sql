IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_CompleteBatchCallLog')
    DROP PROCEDURE usp_CompleteBatchCallLog
GO

CREATE PROCEDURE [dbo].[usp_CompleteBatchCallLog]
    @Id                 INT,
    @ProspectsSucceeded INT,
    @ProspectsFailed    INT,
    @ProspectsSkipped   INT,
    @Status             VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE NaukriBatchCallLog
    SET ProspectsSucceeded = @ProspectsSucceeded,
        ProspectsFailed   = @ProspectsFailed,
        ProspectsSkipped   = @ProspectsSkipped,
        CompletedAt        = GETUTCDATE(),
        Status             = @Status
    WHERE Id = @Id
END
GO
