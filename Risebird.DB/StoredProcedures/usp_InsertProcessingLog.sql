-- usp_InsertProcessingLog
-- Insert a checkpoint/debug log entry for resume processing
CREATE OR ALTER PROCEDURE [dbo].[usp_InsertProcessingLog]
    @SupportTicketId    INT             = NULL,
    @PendingResumeId    INT             = NULL,
    @CandidateResumeId  INT             = NULL,
    @Checkpoint         VARCHAR(100),
    @Status             VARCHAR(20),
    @Message            NVARCHAR(MAX)   = NULL,
    @Details            NVARCHAR(MAX)   = NULL,
    @ProcessedBy        VARCHAR(150)    = NULL,
    @DurationMs         INT             = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dbo].[SupportTicketProcessingLog]
        (SupportTicketId, PendingResumeId, CandidateResumeId, [Checkpoint], [Status], [Message], Details, ProcessedBy, DurationMs)
    VALUES
        (@SupportTicketId, @PendingResumeId, @CandidateResumeId, @Checkpoint, @Status, @Message, @Details, @ProcessedBy, @DurationMs);
    
    SELECT SCOPE_IDENTITY() AS LogId;
END
GO
