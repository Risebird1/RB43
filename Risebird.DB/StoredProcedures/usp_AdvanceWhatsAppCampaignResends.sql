-- WhatsApp campaigns: moves Sent candidates on once the re-send gap has passed without a booking or a
-- confirmation (usp_RefreshWhatsAppCampaignOutcomes runs first and takes the ones who answered):
--   Sends < MaxSends -> Pending, due now (the next pass sends again)
--   otherwise        -> NoResponse ("No reply after n sends")
-- Returns the number of rows moved.
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AdvanceWhatsAppCampaignResends'))
BEGIN
    DROP PROCEDURE dbo.usp_AdvanceWhatsAppCampaignResends
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AdvanceWhatsAppCampaignResends]
    @CampaignId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIME = GETUTCDATE();

    UPDATE cc SET
        [State]     = CASE WHEN cc.Sends < wc.MaxSends THEN 'Pending' ELSE 'NoResponse' END,
        NextSendAt  = CASE WHEN cc.Sends < wc.MaxSends THEN @Now ELSE NULL END,
        OutcomeNote = CASE WHEN cc.Sends < wc.MaxSends THEN cc.OutcomeNote
                           ELSE 'No reply after ' + CAST(cc.Sends AS VARCHAR(10)) + CASE WHEN cc.Sends = 1 THEN ' send' ELSE ' sends' END END,
        UpdatedAt   = @Now
    FROM dbo.WhatsAppCampaignCandidate cc
    INNER JOIN dbo.WhatsAppCampaign wc ON wc.Id = cc.CampaignId
    WHERE cc.CampaignId = @CampaignId
      AND cc.[State] = 'Sent'
      AND cc.LastSentAt <= DATEADD(HOUR, -wc.ResendAfterHours, @Now);

    SELECT @@ROWCOUNT;
END
GO
