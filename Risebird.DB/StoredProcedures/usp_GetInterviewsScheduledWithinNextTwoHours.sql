/****** Object:  StoredProcedure [dbo].[usp_GetInterviewsScheduledWithinNextTwoHours]  ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewsScheduledWithinNextTwoHours'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewsScheduledWithinNextTwoHours
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
  
CREATE PROCEDURE [dbo].[usp_GetInterviewsScheduledWithinNextTwoHours]  
  
@timeNow datetime,
@emailId varchar(100)
  
AS  
Begin  
	--declare @hrId int                
	--set @hrId = (select HR.id from UserList ul join HR on HR.UserId=ul.id where EmailId=@EmailId and idProfile=2)     
	declare @userid int                    
	set @userid = (select id from UserList where EmailId=@EmailId) 
	declare @profileName varchar(30)
	set @profileName = (select pr.[Name] from UserList ul join [Profile] pr on pr.id =ul.idProfile where ul.id = @userid)


  select im.id as imid, im.interviewDate, im.candidateEmail, im.IRFId, im.confirmationStatus, im.interviewer, ulhr.EmailId as ChannelOwner,
  case
	when ca.ClientHRAttemptContact is not Null and ca.ClientHRAttemptContact != '' then ca.ClientHRAttemptContact
	else im.ClientHrContact
  end 
  as ClientHrContact, im.HRCoordinatorEmailId, im.ZoomMeetingId, cl.[Name] as Client, im.candidateName
  from InterviewMeetings im 
  join IRF on irf.IRFId = im.IRFId
  join UserList ul on ul.EmailId = im.candidateEmail
  join CandidatesMaster cm on cm.UserId = ul.id
  join Candidate ca on ca.IRFId = irf.id and  ca.CandidateMasterId = cm.id
  join Client cl on cl.id = irf.ClientId
  left join UserList ulhr on ulhr.id = irf.RisebirdHr
  where im.interviewDate >= @timeNow and im.interviewDate <= DATEADD(HOUR, 2, @timeNow)
  and (((im.HRCoordinatorEmailId = @emailId or irf.RisebirdHr = @userid) and @profileName != 'Admin') or  @profileName = 'Admin')
End  


GO


