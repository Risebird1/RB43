IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateInterviewerAvailbilityStatusonAGLevel'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewerAvailbilityStatusonAGLevel
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_UpdateInterviewerAvailbilityStatusonAGLevel  
@AssesmentGroupId int,  
@uniqueId varchar(50),  
@IsAvailable bit,  
@timeNow datetime,
@rsc varchar(75) = null
AS  
BEGIN  

Declare @InterviewerId int  

Select @InterviewerId=id From InterviewerProfile where UserId=(Select id from UserList where uniqueid=@uniqueid)
if(@IsAvailable = 1)
Begin
	if not exists(select top 1 id from AGLevelAvailability where AssessmentGroupId =@AssesmentGroupId and InterviewerProfileId = @InterviewerId)
	Begin
		Insert into AGLevelAvailability(AssessmentGroupId, InterviewerProfileId, IsAvailable, [Source], CreatedOn)   
		values(@AssesmentGroupId, @InterviewerId, 1, @rsc, @timeNow)  
	End
    else
	Begin
		update AGLevelAvailability set IsAvailable = 1, ModifiedOn = @timeNow, [Source] = @rsc where AssessmentGroupId =@AssesmentGroupId and InterviewerProfileId = @InterviewerId
	End
  
End
else
Begin

	if not exists(select top 1 id from AGLevelAvailability where AssessmentGroupId =@AssesmentGroupId and InterviewerProfileId = @InterviewerId)
	Begin
		Insert into AGLevelAvailability(AssessmentGroupId, InterviewerProfileId, IsAvailable, [Source], CreatedOn)   
		values(@AssesmentGroupId, @InterviewerId, 0, @rsc, @timeNow)  
	End
	else
	Begin
		update AGLevelAvailability set IsAvailable = 0, ModifiedOn = @timeNow, [Source] = @rsc where AssessmentGroupId =@AssesmentGroupId and InterviewerProfileId = @InterviewerId
	End
End
--the response is either yes or no, if there is no response, the entry will not exist in the table 
END  
Go