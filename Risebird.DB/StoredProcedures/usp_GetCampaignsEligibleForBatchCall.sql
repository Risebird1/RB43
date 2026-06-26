CREATE OR ALTER PROCEDURE [dbo].[usp_GetCampaignsEligibleForBatchCall]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.id              AS CampaignId,
        c.CampaignName,
        c.AssessmentGroupId,
        c.CreatedAt,
        ISNULL(mag.MAGName, ag.AssessmentGroup) AS MagName
    FROM AGCampaigns c
    INNER JOIN AssessmentGroup ag ON ag.id = c.AssessmentGroupId
    LEFT JOIN MasterAssessmentGroup mag
        ON mag.AssessmentGroupId = c.AssessmentGroupId
        AND ISNULL(mag.IsDeleted, 0) = 0
    WHERE c.BatchCallTriggeredAt IS NULL
      -- CreatedAt is stored from the app's DateTime.Now (IST). GETDATE() on this server returns UTC
      -- (the rest of the codebase converts via DATEADD(MINUTE, 330, GETDATE())), so comparing against
      -- bare GETDATE() made IST timestamps look ~5.5h "in the future" and a campaign would not become
      -- eligible until ~6.5h after creation. Compare against IST instead.
      AND c.CreatedAt <= DATEADD(HOUR, -1, DATEADD(MINUTE, 330, GETUTCDATE()))
END
GO
