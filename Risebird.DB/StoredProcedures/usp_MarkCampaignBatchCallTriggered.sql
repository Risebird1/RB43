CREATE OR ALTER PROCEDURE [dbo].[usp_MarkCampaignBatchCallTriggered]
    @CampaignId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE AGCampaigns
    SET BatchCallTriggeredAt = GETDATE()
    WHERE id = @CampaignId;
END
GO
