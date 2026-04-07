-- usp_GetProcessingLogs
-- Get processing logs for debugging, optionally filtered by ticket/pending resume
CREATE OR ALTER PROCEDURE [dbo].[usp_GetProcessingLogs]
    @SupportTicketId    INT = NULL,
    @PendingResumeId    INT = NULL,
    @Limit              INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@Limit)
        Id,
        SupportTicketId,
        PendingResumeId,
        CandidateResumeId,
        [Checkpoint],
        [Status],
        [Message],
        Details,
        ProcessedBy,
        CreatedAt,
        DurationMs
    FROM [dbo].[SupportTicketProcessingLog]
    WHERE
        (@SupportTicketId IS NULL OR SupportTicketId = @SupportTicketId)
        AND (@PendingResumeId IS NULL OR PendingResumeId = @PendingResumeId)
    ORDER BY CreatedAt DESC;
END
GO
