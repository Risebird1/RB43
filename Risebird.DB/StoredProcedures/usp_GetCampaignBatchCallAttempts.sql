-- Returns every batch-call attempt made for a campaign, newest first, for the Campaigns-page history.
CREATE OR ALTER PROCEDURE [dbo].[usp_GetCampaignBatchCallAttempts]
    @CampaignId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        l.Id,
        l.CampaignId,
        l.AttemptNumber,
        l.AttemptName,
        l.MagName,
        l.InitiatedBy,
        l.InitiatedAt,
        l.InterviewersAttempted,
        l.InterviewersSucceeded,
        l.InterviewersFailed,
        l.InterviewersSkipped,
        l.CompletedAt,
        l.Status
    FROM IABatchCallLog l
    WHERE l.CampaignId = @CampaignId
    ORDER BY l.Id DESC;
END
GO
