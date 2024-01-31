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

Declare @InterviewerId int, @userId int  

Select @userId = id From UserList where uniqueid=@uniqueid

Select @InterviewerId=id From InterviewerProfile where UserId=@userId

declare @Enumerator table (id int)  
  
insert into @Enumerator  
select distinct IRFId from CampaignSkillChannels   
where CampaignId = @campaignId 

declare @irfId int 

if(@IsAvailable = 1)
Begin

	update InterviewerProfile set  AvailabilityResponseDate = @timeNow, Isavailable = 1 where UserId = @userId  


	
  
	while exists (select 1 from @Enumerator)  
	begin  
		select top 1 @irfId = id from @Enumerator

		if not exists(select top 1 id from TransInterviewer where IRFId =@irfId and UserId = @UserId)
		Begin
			Insert into TransInterviewer(IRFId,InterviewerId,UserId,IsActive,IsAvailable, PerSuccessRate, [Source], IsCampaignActivity, CampaignId)   
			values(@IRFId,@InterviewerId,@UserId,1,1, (select FlatRate from IRF where id=@IRFId), @rsc, 1, @campaignId) 
		
		End
		Else
		Begin
			update TransInterviewer set IsActive = 1, IsAvailable=1, [Source] = @rsc, IsCampaignActivity = 1, CampaignId =@campaignId  where IRFId =@irfId and UserId = @UserId
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