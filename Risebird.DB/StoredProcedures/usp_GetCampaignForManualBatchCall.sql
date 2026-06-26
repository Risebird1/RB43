-- Resolves a single campaign for an OPERATOR-INITIATED (manual) batch call.
--
-- Unlike usp_GetCampaignsEligibleForBatchCall (used by the automated timer job), this deliberately
-- has NO 1-hour-after-creation time gate: a manual trigger is an explicit human override, so the
-- operator can place the availability calls immediately. The only thing that blocks a manual run is
-- the batch call having ALREADY been triggered (BatchCallTriggeredAt IS NOT NULL), which prevents a
-- duplicate run.
CREATE OR ALTER PROCEDURE [dbo].[usp_GetCampaignForManualBatchCall]
    @CampaignId INT
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
    WHERE c.id = @CampaignId
      AND c.BatchCallTriggeredAt IS NULL;
END
GO
