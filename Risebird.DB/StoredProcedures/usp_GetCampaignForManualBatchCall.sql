-- Resolves a single campaign for an OPERATOR-INITIATED (manual) batch call.
--
-- Multiple batch-call attempts per campaign are allowed (each is logged separately), so there is NO
-- "already triggered" gate and NO 1-hour delay gate (the delay only governs the automated timer job).
-- The only thing the caller should refuse on is HasRunningAttempt = 1, i.e. an attempt for this
-- campaign is still in progress — to avoid dialing the same interviewers twice at once.
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
        ISNULL(mag.MAGName, ag.AssessmentGroup) AS MagName,
        CASE WHEN EXISTS (
            SELECT 1 FROM IABatchCallLog l
            WHERE l.CampaignId = c.id AND l.CompletedAt IS NULL AND l.Status = 'Running'
        ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasRunningAttempt
    FROM AGCampaigns c
    INNER JOIN AssessmentGroup ag ON ag.id = c.AssessmentGroupId
    LEFT JOIN MasterAssessmentGroup mag
        ON mag.AssessmentGroupId = c.AssessmentGroupId
        AND ISNULL(mag.IsDeleted, 0) = 0
    WHERE c.id = @CampaignId;
END
GO
