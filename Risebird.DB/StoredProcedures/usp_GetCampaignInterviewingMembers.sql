IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCampaignInterviewingMembers'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCampaignInterviewingMembers
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetCampaignInterviewingMembers      
@campaignId int      
AS      
BEGIN      


Select distinct ul.EmailId, ul.uniqueid from InterviewerProfile ipr cross Apply string_split(AssessmentGroupId,',') 
join AGCampaigns agc on agc.AssessmentGroupId = value 
join UserList ul on ul.id = ipr.UserId
where agc.id = @campaignId and ul.IsActive = 1
  
END
GO

  
      