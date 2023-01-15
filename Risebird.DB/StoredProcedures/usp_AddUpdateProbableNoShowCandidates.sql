/****** Object:  StoredProcedure [dbo].[usp_AddUpdateProbableNoShowCandidates]   ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddUpdateProbableNoShowCandidates'))
BEGIN
    DROP PROCEDURE dbo.usp_AddUpdateProbableNoShowCandidates
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
CREATE PROCEDURE [dbo].[usp_AddUpdateProbableNoShowCandidates]    
@currentTime datetime  
AS    
Begin   

declare @dateOfExistingRecords datetime = (select top 1 CreatedAt from ProbableNoShowCandidates)

if( @dateOfExistingRecords is not null and (cast(@dateOfExistingRecords as date) != cast(@currentTime as date)))
Begin
	delete from ProbableNoShowCandidates where (cast(@dateOfExistingRecords as date) != cast(@currentTime as date))
End
--delete from ProbableNoShowCandidates where id in  
--(select pns.id from ProbableNoShowCandidates pns join InterviewMeetings im on im.id = pns.InterviewId where im.confirmationStatus = 2 or im.interviewDate < @currentTime)  
  
  
declare @Enumerator table (id int)  
  
insert into @Enumerator  
select distinct id from InterviewMeetings   
where interviewDate >= @currentTime and interviewDate <= dateadd(MINUTE, 60, @currentTime)  
and confirmationStatus = 1  
  
declare @interviewId int,  @candidateEmailId varchar(100)--,@isProbableNoShow bit = 1, @isMailDelivered bit  
  
while exists (select 1 from @Enumerator)  
begin  
     select top 1 @interviewId = id from @Enumerator   
  set @candidateEmailId = (select candidateEmail from InterviewMeetings where id = @interviewId)  
       
  if exists(select top 1 smd.id from SentMailDetails smd   
  join SendgridEventData sge on sge.Split_Message_Id = smd.sg_message_id   
  where smd.InterviewId = @interviewId and sge.email = @candidateEmailId and sge.[event] = 'open')  
  Begin  
  
	  delete from ProbableNoShowCandidates where InterviewId = @interviewId  
	  delete from @Enumerator where id = @interviewId  
  continue  
  End  
  Else if exists(select top 1 id from CallRecordingData crd where CallRecipient = @candidateEmailId and InterviewId = @interviewId and [Status] = 'completed' and Duration > 0 )--and RecordingUri is not null  
  Begin  
	  delete from ProbableNoShowCandidates where InterviewId = @interviewId  
	  delete from @Enumerator where id = @interviewId  
  continue  
  End  
    
  --if the mail has not been delivered or a call could not be connected, then the chances of no show are high  
  
     delete from @Enumerator where id = @interviewId  
  if not exists(select top 1 id from ProbableNoShowCandidates where interviewId = @interviewId)
  Begin
    insert into ProbableNoShowCandidates values ((select id from IRF where IRFId = (select IRFId from InterviewMeetings where id =@interviewId)), (select id from UserList where EmailId = @candidateEmailId and idProfile =23), @interviewId, dateadd(MINUTE, 330, getdate()), null)  
  End
  --set @isProbableNoShow = 1  
end  
  
End  
GO

