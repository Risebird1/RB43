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
  
  
--if block to check if the campaign is created against a master assessment group  
if exists (select id from MasterAssessmentGroup where AssessmentGroupId = (select top 1 AssessmentGroupId from AGCampaigns where id = @campaignId))  
Begin  
  
 --get the list of all the assessment groups now  
 DECLARE @tbl_AllAssessmentGroups TABLE (            
  id INT          
 )  
  
 declare @magId int  
 set @magId = (select top 1 id from MasterAssessmentGroup where AssessmentGroupId = (select top 1 AssessmentGroupId from AGCampaigns where id = @campaignId))  
  
 insert into @tbl_AllAssessmentGroups  
 select distinct AssessmentGroupId from SecondaryAssessmentGroups where MAGId =@magId  
 union  
 select top 1 AssessmentGroupId from MasterAssessmentGroup where id = @magId  
  
  
 Select distinct ul.EmailId, ul.uniqueid from InterviewerProfile ipr cross Apply string_split(AssessmentGroupId,',')   
 join UserList ul on ul.id = ipr.UserId  
 where value in (select distinct id from @tbl_AllAssessmentGroups) and ul.IsActive = 1 and ISNULL(ipr.IsDeleted,0)=0 
  
  
End  
Else  
Begin  
  
 Select distinct ul.EmailId, ul.uniqueid from InterviewerProfile ipr cross Apply string_split(AssessmentGroupId,',')   
 join CampaignAssessmentGroups cag on cag.AssessmentGroupId = value  
 join UserList ul on ul.id = ipr.UserId  
 where cag.CampaignId = @campaignId and ul.IsActive = 1 and ISNULL(ipr.IsDeleted,0)=0 
  
End  
  
    
END  
GO

  
      