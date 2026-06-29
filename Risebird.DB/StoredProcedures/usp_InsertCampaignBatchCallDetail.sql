-- Records the outcome of a single call placed during a batch-call attempt.
CREATE OR ALTER PROCEDURE [dbo].[usp_InsertCampaignBatchCallDetail]
    @BatchCallLogId       INT,
    @InterviewerProfileId INT = NULL,
    @InterviewerName      NVARCHAR(200) = NULL,
    @InterviewerEmail     NVARCHAR(256) = NULL,
    @Phone                VARCHAR(20) = NULL,
    @Outcome              VARCHAR(20),
    @Error                NVARCHAR(1000) = NULL,
    @ConversationId       VARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO IABatchCallAttemptDetail
        (BatchCallLogId, InterviewerProfileId, InterviewerName, InterviewerEmail, Phone, Outcome, Error, ConversationId)
    VALUES
        (@BatchCallLogId, @InterviewerProfileId, @InterviewerName, @InterviewerEmail, @Phone, @Outcome, @Error, @ConversationId);
END
GO
