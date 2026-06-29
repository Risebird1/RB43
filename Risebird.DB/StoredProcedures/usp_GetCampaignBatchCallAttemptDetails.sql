-- Returns the per-call detail rows for a single batch-call attempt (drill-down on the Campaigns page).
CREATE OR ALTER PROCEDURE [dbo].[usp_GetCampaignBatchCallAttemptDetails]
    @BatchCallLogId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.Id,
        d.BatchCallLogId,
        d.InterviewerProfileId,
        d.InterviewerName,
        d.InterviewerEmail,
        d.Phone,
        d.Outcome,
        d.Error,
        d.ConversationId,
        d.CalledAt
    FROM IABatchCallAttemptDetail d
    WHERE d.BatchCallLogId = @BatchCallLogId
    ORDER BY d.Id ASC;
END
GO
