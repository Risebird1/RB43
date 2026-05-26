CREATE OR ALTER PROCEDURE [dbo].[usp_CompleteIABatchCallLog]
    @Id        INT,
    @Succeeded INT,
    @Failed    INT,
    @Skipped   INT,
    @Status    VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE IABatchCallLog
    SET InterviewersSucceeded = @Succeeded,
        InterviewersFailed    = @Failed,
        InterviewersSkipped   = @Skipped,
        CompletedAt           = GETUTCDATE(),
        Status                = @Status
    WHERE Id = @Id;
END
GO
