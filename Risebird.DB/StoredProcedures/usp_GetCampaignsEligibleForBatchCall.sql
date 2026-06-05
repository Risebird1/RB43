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
      AND c.CreatedAt <= DATEADD(HOUR, -1, GETDATE())
END
GO
