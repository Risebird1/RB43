CREATE OR ALTER PROCEDURE [dbo].[usp_GetCampaignUnrespondedInterviewersForBatchCall]
    @CampaignId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Collect all assessment group IDs for this campaign (MAG-aware, same logic as usp_GetCampaignInterviewingMembers)
    DECLARE @tbl_AllAssessmentGroups TABLE (id INT);
    DECLARE @AssessmentGroupId INT;

    SELECT @AssessmentGroupId = AssessmentGroupId FROM AGCampaigns WHERE id = @CampaignId;

    IF EXISTS (SELECT 1 FROM MasterAssessmentGroup WHERE AssessmentGroupId = @AssessmentGroupId AND ISNULL(IsDeleted, 0) = 0)
    BEGIN
        DECLARE @magId INT;
        SELECT TOP 1 @magId = id FROM MasterAssessmentGroup
        WHERE AssessmentGroupId = @AssessmentGroupId AND ISNULL(IsDeleted, 0) = 0;

        INSERT INTO @tbl_AllAssessmentGroups
        SELECT DISTINCT AssessmentGroupId FROM SecondaryAssessmentGroups WHERE MAGId = @magId
        UNION
        SELECT AssessmentGroupId FROM MasterAssessmentGroup WHERE id = @magId;
    END
    ELSE
    BEGIN
        INSERT INTO @tbl_AllAssessmentGroups
        SELECT DISTINCT AssessmentGroupId FROM CampaignAssessmentGroups WHERE CampaignId = @CampaignId;
    END

    -- Get all targeted interviewers who have NOT responded to this campaign
    -- "Responded" = has a TransInterviewer row with IsCampaignActivity=1 AND CampaignId=@CampaignId
    SELECT DISTINCT
        ipr.Id              AS InterviewerProfileId,
        ipr.NickName        AS InterviewerName,
        ul.EmailId          AS SenderEmail,
        ul.MobileNumber     AS Phone,
        ul.Id               AS UserId
    FROM InterviewerProfile ipr
    CROSS APPLY STRING_SPLIT(ipr.AssessmentGroupId, ',') s
    INNER JOIN UserList ul ON ul.Id = ipr.UserId
    WHERE s.value IN (SELECT CAST(id AS VARCHAR(20)) FROM @tbl_AllAssessmentGroups)
      AND ul.IsActive = 1
      AND ISNULL(ipr.IsDeleted, 0) = 0
      AND ISNULL(ipr.Isavailable, 0) <> 4   -- not dropped
      AND LEN(ISNULL(ul.MobileNumber, '')) >= 10
      AND NOT EXISTS (
          SELECT 1 FROM TransInterviewer ti
          WHERE ti.UserId = ul.Id
            AND ti.IsCampaignActivity = 1
            AND ti.CampaignId = @CampaignId
      )
      -- Exclude "recently available": signaled a favorable availability within the last 48h
      -- for any MAG belonging to this campaign's assessment groups.
      AND NOT EXISTS (
          SELECT 1
          FROM InterviewerEmailAvailability ea
          INNER JOIN MasterAssessmentGroup mag
              ON mag.MAGName = ea.MagName AND ISNULL(mag.IsDeleted, 0) = 0
          WHERE (ea.UserId = ul.Id OR ea.InterviewerProfileId = ipr.Id OR ea.SenderEmail = ul.EmailId)
            AND mag.AssessmentGroupId IN (SELECT id FROM @tbl_AllAssessmentGroups)
            AND ea.ReceivedAt >= DATEADD(HOUR, -48, GETUTCDATE())
            AND dbo.fn_IsFavorableAvailabilityIntent(ea.AvailabilityIntent, ea.MailBodyIntent, ea.ConcernsOrNotInterested) = 1
      )
END
GO
