/****** Object:  StoredProcedure [dbo].[usp_AddUpdateProbableNoShowInterviewers]   ******/
--run this every 25 minutes
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddUpdateProbableNoShowInterviewers'))
BEGIN
    DROP PROCEDURE dbo.usp_AddUpdateProbableNoShowInterviewers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
    
CREATE PROCEDURE [dbo].[usp_AddUpdateProbableNoShowInterviewers]      
@currentTimeParam datetime    
AS      
Begin     
  
   
declare @Enumerator table (id int)    
    
insert into @Enumerator    
select distinct id from InterviewMeetings     
where interviewDate >= @currentTimeParam and interviewDate <= dateadd(MINUTE, 60, @currentTimeParam)    
and confirmationStatus = 1    
    
declare @interviewId int,  @interviewerEmailId varchar(100)--,@isProbableNoShow bit = 1, @isMailDelivered bit    
    
while exists (select 1 from @Enumerator)    
begin    
     select top 1 @interviewId = id from @Enumerator     
  set @interviewerEmailId = (select interviewer from InterviewMeetings im join UserList ul on ul.EmailId = im.interviewer where im.id = @interviewId and ul.idProfile = 3)    
      
	--check for last login details first, if has not logged in for the past week?, days?, months?	
  if((select top 1 DATEDIFF(MINUTE, LastLogin, @currentTimeParam) from UserList where EmailId = @interviewerEmailId and idProfile =3) <= 60)
  Begin

	delete from ProbableNoShowInterviewers where InterviewId = @interviewId    
	delete from @Enumerator where id = @interviewId    
	continue

  End
  if exists(select top 1 smd.id from SentMailDetails smd     
  join SendgridEventData sge on sge.Split_Message_Id = smd.sg_message_id     
  where smd.InterviewId = @interviewId and sge.email = @interviewerEmailId and sge.[event] = 'open')    
  Begin    
    
   delete from ProbableNoShowInterviewers where InterviewId = @interviewId    
   delete from @Enumerator where id = @interviewId    
  continue    
  End    
  Else if exists(select top 1 id from CallRecordingData crd where CallRecipient = @interviewerEmailId and InterviewId = @interviewId and [Status] = 'completed' and Duration > 0 )--and RecordingUri is not null    
  Begin    
   delete from ProbableNoShowInterviewers where InterviewId = @interviewId    
   delete from @Enumerator where id = @interviewId    
  continue    
  End    
      
  --if the mail has not been delivered or a call could not be connected, then the chances of no show are high    
    
     delete from @Enumerator where id = @interviewId    
  if not exists(select top 1 id from ProbableNoShowInterviewers where interviewId = @interviewId)  
  Begin  
   if exists(select top 1 id from UserList where emailid = @interviewerEmailId)
   Begin
       insert into ProbableNoShowInterviewers values ((select id from IRF where IRFId = (select IRFId from InterviewMeetings where id =@interviewId)), (select id from UserList where EmailId = @interviewerEmailId), @interviewId, @currentTimeParam, null)    

   End
  End  
  --set @isProbableNoShow = 1    
end    
    
End    