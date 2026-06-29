IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCampaignDetailsById'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetCampaignDetailsById]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetCampaignDetailsById]               
@campaignId int    
AS                
BEGIN 

--campaign table
-- LEFT JOIN to AssessmentGroup so a valid campaign always returns a header row even if its
-- assessment group is missing/removed (an INNER JOIN previously dropped the row entirely, which
-- left the caller with a null model and crashed the details page).
select agc.id, agc.CampaignName, agc.CreatedAt, agc.CreatedBy, agc.ModifiedAt, agc.ModifiedBy, 
ag.AssessmentGroup as AssessmentGroupName, 
agc.AssessmentGroupId,
CASE
        WHEN mag.id is null THEN 0
        WHEN mag.id > 0 THEN 1
        ELSE 0
END AS IsAlreadyMasterAG
from AGCampaigns agc
left join AssessmentGroup ag on ag.id = agc.AssessmentGroupId
left join MasterAssessmentGroup mag on mag.AssessmentGroupId = agc.AssessmentGroupId
where agc.id =@campaignId
     
--campaign target assessment groups table 
select ag.AssessmentGroup, ag.id from CampaignAssessmentGroups cag
join AssessmentGroup ag on ag.id = cag.AssessmentGroupId
where cag.CampaignId =@campaignId

--campaign target skill channels
select irf.id, irf.IRFId, ag.AssessmentGroup as AssessmentGroupName from CampaignSkillChannels csc
join IRF on irf.id = csc.IRFId
join AssessmentGroup ag on ag.id = irf.AssessmentGroupId
where csc.CampaignId =@campaignId


--campaign interviewers targeted
EXEC usp_GetCampaignInterviewingMembers @campaignId

END


GO

    
      