IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateInterviewerAvailbilityStatusCampaignLevel'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewerAvailbilityStatusCampaignLevel  
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_UpdateInterviewerAvailbilityStatusCampaignLevel    
@campaignId int,    
@uniqueId varchar(50),    
@IsAvailable bit,    
@timeNow datetime,  
@rsc varchar(75) = null  
AS    
BEGIN    
  
Declare @InterviewerId int, @userId int, @assessmentGroupId varchar(20)    
  
Select @userId = id From UserList where uniqueid=@uniqueid  
  
Select @InterviewerId=id From InterviewerProfile where UserId=@userId  
  
Select @assessmentGroupId=AssessmentGroupId From AGCampaigns where id=@campaignId  
  
declare @interviewerAssessmentGroups varchar(500)  
  
select @interviewerAssessmentGroups = AssessmentGroupId from InterviewerProfile where UserId= @userId

declare @Enumerator table (id int)    
    
insert into @Enumerator    
select distinct IRFId from CampaignSkillChannels     
where CampaignId = @campaignId   
  
declare @irfId int   
  
if(@IsAvailable = 1)  
Begin  
  
 update InterviewerProfile set  AvailabilityResponseDate = @timeNow, Isavailable = 1 where UserId = @userId    
  

  declare @campaignAssessmentGroupstbl table (id int)    
    
	insert into @campaignAssessmentGroupstbl    
	select distinct AssessmentGroupId from CampaignAssessmentGroups     
	where CampaignId = @campaignId 
	
	declare @currentAssessmentGrp varchar(10)

	declare @interviewerAsgGrpstbl table (id int)
	insert into @interviewerAsgGrpstbl    
	Select top 1 id from InterviewerProfile ipr cross Apply string_split(AssessmentGroupId,',')   
	where UserId = @userId

while exists (select 1 from @campaignAssessmentGroupstbl)    
begin    
	select top 1 @currentAssessmentGrp = id from @campaignAssessmentGroupstbl
	
	if not exists( Select top 1 id from @interviewerAsgGrpstbl   
	where id = @currentAssessmentGrp)  
	Begin  
	  --select @interviewerAssessmentGroups = AssessmentGroupId from InterviewerProfile where UserId= @userId  
  
	  if(@interviewerAssessmentGroups is null or @interviewerAssessmentGroups = '')  
	  Begin  
	  set @interviewerAssessmentGroups = @currentAssessmentGrp  
	  End  
	  else  
	  begin  
	  set @interviewerAssessmentGroups = @interviewerAssessmentGroups + ',' + @currentAssessmentGrp  
	  end  


	  delete from @campaignAssessmentGroupstbl where id = @currentAssessmentGrp
	 End

	 update InterviewerProfile set AssessmentGroupId =@interviewerAssessmentGroups where UserId = @userId  


end

  
   
    
 while exists (select 1 from @Enumerator)    
 begin    
  select top 1 @irfId = id from @Enumerator  
  
  if not exists(select top 1 id from TransInterviewer where IRFId =@irfId and UserId = @UserId)  
  Begin 
  
  declare @fltRate int
  set @fltRate = (select FlatRate from IRF where id=@IRFId)
  if(@fltRate is null or @fltRate = 0)
  begin
	set @fltRate = 420
  end

   Insert into TransInterviewer(IRFId,InterviewerId,UserId, IsActive, IsAvailable, PerSuccessRate, [Source], IsCampaignActivity, CampaignId)     
   values(@IRFId,@InterviewerId,@UserId,0,1, @fltRate, @rsc, 1, @campaignId)   
    
  End  
  Else  
  Begin  
   update TransInterviewer set IsAvailable=1, [Source] = @rsc, IsCampaignActivity = 1, CampaignId =@campaignId  where IRFId =@irfId and UserId = @UserId  --IsActive = 1, 
  End  
  
  delete from @Enumerator where id = @irfId  
  
 End  
   
End  
else  
Begin  
 update InterviewerProfile set  AvailabilityResponseDate = @timeNow, Isavailable = 2 where UserId = @userId     
  
 while exists (select 1 from @Enumerator)    
 begin    
  select top 1 @irfId = id from @Enumerator  
    
  Update ti set ti.IsActive=0,IsAvailable=0, ModifiedBy = @rsc, IsCampaignActivity =1, CampaignId = @campaignId from TransInterviewer ti  Where IRFId=@IRFId and UserId =@userId   
     
  delete from @Enumerator where id = @irfId  
  
 End  
    
    
End  
--the response is either yes or no, if there is no response, the entry will not exist in the table   
END    
Go