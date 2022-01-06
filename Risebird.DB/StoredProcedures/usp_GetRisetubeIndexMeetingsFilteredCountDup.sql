

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRisetubeIndexMeetingsFilteredCountDup'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRisetubeIndexMeetingsFilteredCountDup
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
  
    
    
CREATE PROCEDURE [dbo].[usp_GetRisetubeIndexMeetingsFilteredCountDup]      
@searchValueParam varchar(max) = null,    
@clientIdParam int,    
@EmailIdParam varchar(100),    
@startDateParam datetime,    
@endDateParam datetime    
    
    
AS      
Begin     
declare @totalResults int, @searchValue varchar(max), @Emailid varchar(100), @clientid int,    
  @startDate datetime, @endDate datetime    
  Declare @Role varchar(50)  
  set @searchValue=@searchValueParam    
  set @Emailid=@EmailIdParam    
  set @clientid=@clientIdParam    
  set @startDate=@startDateParam    
  set @endDate=@endDateParam    
    
    
    
if @clientid = 0    
 Begin    
 Select @Role=pr.Name from UserList ul join Profile pr on ul.idProfile=pr.id where ul.EmailId=@Emailid  
 if(@Role='SeniorHR')  
 Begin  
 if(@searchValue is not null)    
   Begin    
    set @totalResults=(select count(a.id) from (select im.id from InterviewMeetings im     
      inner join IRF on im.IRFId = IRF.IRFId    
      left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId          
      left join UserList ul on ul.EmailId = im.candidateEmail          
      join CandidatesMaster cm on ul.id = cm.UserId          
      join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id          
      left join QCFeedbackReport QCF on im.id=QCF.InterviewId          
      inner join client cl on IRF.ClientID=cl.id    
   join SeniorHR sh on sh.Clientid=IRF.ClientId  
      left join Userlist ulsh on ulsh.id=sh.Userid  
      outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and NULLIF(VideoLink, '') IS NOT NULL)patchvideoCount    
      outer apply (select sum(WeightageActual) as TechSkillScore from TechSkillReport where Interviewid=ic.InterviewId )Techscore    
      where ic.MeetingStatus=4 and (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and (im.candidateName like ('%' +@searchValue +'%') or irf.JDTiltle like ('%' +@searchValue +'%'))     
      and cast(interviewDate as date) >= cast (@startDate as date)    
      and cast(interviewDate as date) <= cast (@endDate as date)    
      and ulsh.EmailId=@Emailid      
      group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,          
      im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,          
      im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,          
      im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.JoiningBiometryScore,           
      ca.Phone, ca.ResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage ,cl.CuratedLink , QCF.InterviewId ,ic.CandidateAvailable,patchvideoCount.MeetingCount,Techscore.TechSkillScore, ic.ThumbnailTimeSeconds) a)    
    
   End    
  Else    
   Begin    
     set @totalResults=(select count(a.id) from (select im.id from InterviewMeetings im     
      inner join IRF on im.IRFId = IRF.IRFId            
      left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId          
      left join UserList ul on ul.EmailId = im.candidateEmail          
      join CandidatesMaster cm on ul.id = cm.UserId          
      join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id          
      left join QCFeedbackReport QCF on im.id=QCF.InterviewId          
      inner join client cl on IRF.ClientID=cl.id     
   join SeniorHR sh on sh.Clientid=IRF.ClientId  
      left join Userlist ulsh on ulsh.id=sh.Userid  
      outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and NULLIF(VideoLink, '') IS NOT NULL)patchvideoCount    
      outer apply (select sum(WeightageActual) as TechSkillScore from TechSkillReport where Interviewid=ic.InterviewId )Techscore    
      where ic.MeetingStatus=4 and (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null    
      and cast(interviewDate as date) >= cast (@startDate as date)    
      and cast(interviewDate as date) <= cast (@endDate as date)    
      and ulsh.EmailId=@Emailid      
      group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,          
      im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,          
      im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,          
      im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.JoiningBiometryScore,           
      ca.Phone, ca.ResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage ,cl.CuratedLink , QCF.InterviewId ,ic.CandidateAvailable,patchvideoCount.MeetingCount,Techscore.TechSkillScore, ic.ThumbnailTimeSeconds) a)    
   End    
 End  
 Else  
 Begin  
  if(@searchValue is not null)    
   Begin    
    set @totalResults=(select count(a.id) from (select im.id from InterviewMeetings im     
      inner join IRF on im.IRFId = IRF.IRFId    
      left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId          
      left join UserList ul on ul.EmailId = im.candidateEmail          
      join CandidatesMaster cm on ul.id = cm.UserId          
      join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id          
      left join QCFeedbackReport QCF on im.id=QCF.InterviewId          
      inner join client cl on IRF.ClientID=cl.id    
      outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and NULLIF(VideoLink, '') IS NOT NULL)patchvideoCount    
      outer apply (select sum(WeightageActual) as TechSkillScore from TechSkillReport where Interviewid=ic.InterviewId )Techscore    
      where ic.MeetingStatus=4 and (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and (im.candidateName like ('%' +@searchValue +'%') or irf.JDTiltle like ('%' +@searchValue +'%'))     
      and cast(interviewDate as date) >= cast (@startDate as date)    
      and cast(interviewDate as date) <= cast (@endDate as date)    
          
      group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,          
      im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,          
      im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,          
      im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.JoiningBiometryScore,           
      ca.Phone, ca.ResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage ,cl.CuratedLink , QCF.InterviewId ,ic.CandidateAvailable,patchvideoCount.MeetingCount,Techscore.TechSkillScore, ic.ThumbnailTimeSeconds) a)    
    
   End    
  Else    
   Begin    
     set @totalResults=(select count(a.id) from (select im.id from InterviewMeetings im     
      inner join IRF on im.IRFId = IRF.IRFId            
      left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId          
      left join UserList ul on ul.EmailId = im.candidateEmail          
      join CandidatesMaster cm on ul.id = cm.UserId          
      join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id     
      left join QCFeedbackReport QCF on im.id=QCF.InterviewId          
      inner join client cl on IRF.ClientID=cl.id     
      outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and NULLIF(VideoLink, '') IS NOT NULL)patchvideoCount    
      outer apply (select sum(WeightageActual) as TechSkillScore from TechSkillReport where Interviewid=ic.InterviewId )Techscore    
      where ic.MeetingStatus=4 and (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null    
      and cast(interviewDate as date) >= cast (@startDate as date)    
      and cast(interviewDate as date) <= cast (@endDate as date)    
          
      group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,          
      im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,          
      im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,          
      im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.JoiningBiometryScore,           
      ca.Phone, ca.ResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage ,cl.CuratedLink , QCF.InterviewId ,
ic.CandidateAvailable,patchvideoCount.MeetingCount,Techscore.TechSkillScore, ic.ThumbnailTimeSeconds) a)    
   End    
  End  
 End    
    
Else    
 Begin    
    
  declare @tblTechSkill table (InterviewId int, TechSkillScore float);    
   insert into @tblTechSkill    
   select InterviewId, sum(WeightageActual) as TechSkillScore from TechSkillReport group by InterviewId    
    
  declare @tblPatchCount table (InterviewId int, MeetingPatchCount int);    
   insert into @tblPatchCount    
   select InterviewId, count(InterviewId)  from VideoQuestionDetails where NULLIF(VideoLink, '') IS NOT NULL group by InterviewId    
    
	declare @Roleid int
	Select @Roleid=RoleId From ClientUser cu join Userlist ul on cu.UserId=ul.id where Emailid=@Emailid

if(@Roleid=1)
BEGIN
if(@searchValue is not null)    
    
   Begin    
    set @totalResults=(select count(a.id) from (select im.id from InterviewMeetings im     
      inner join [dbo].[InterviewComments] ic on im.id = ic.InterviewId          
      inner join UserList ulc on ulc.EmailId = im.candidateEmail          
      inner join CandidatesMaster cm on ulc.id = cm.UserId          
      left join QCFeedbackReport QCF on im.id=QCF.InterviewId          
      inner join IRF on im.IRFId = IRF.IRFId              
      inner join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id          
      inner join dbo.UserList ul on ul.id = tc.ClientUserId          
      inner join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id      
      inner join @tblTechSkill tts on tts.InterviewId = im.id    
      left join @tblPatchCount tpc on tpc.InterviewId=ic.InterviewId    
      where ic.MeetingStatus=4 and (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and ul.EmailId=@Emailid 
	  and ((irf.IsQCRequired=1 and QCF.QCStatus=1) or irf.IsQCRequired=0) 
	  and (ic.MoveToCP is null or ic.MoveToCP =0) and (im.candidateName like ('%' +@searchValue +'%') or irf.JDTiltle like ('%' +@searchValue +'%'))    
      and irf.IsDeleted=0 and im.ClientHrContact=@Emailid   
      and cast(interviewDate as date) >= cast (@startDate as date)    
      and cast(interviewDate as date) <= cast (@endDate as date)    
      group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,          
      im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,          
      im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,          
      im.currentEmployerName, im.QCInterviewer, im.IsEmergency,  im.JoiningBiometryScore,           
      ca.Phone, ca.ResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage  , QCF.InterviewId ,ic.CandidateAvailable,tpc.MeetingPatchCount,tts.TechSkillScore, ic.ThumbnailTimeSeconds) a)    
    
   End    
  Else    
   Begin    
    set @totalResults=(select count(a.id) from (select im.id from InterviewMeetings im     
      inner join [dbo].[InterviewComments] ic on im.id = ic.InterviewId          
      inner join UserList ulc on ulc.EmailId = im.candidateEmail          
      inner join CandidatesMaster cm on ulc.id = cm.UserId          
      left join QCFeedbackReport QCF on im.id=QCF.InterviewId          
      inner join IRF on im.IRFId = IRF.IRFId              
      inner join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id          
      inner join dbo.UserList ul on ul.id = tc.ClientUserId          
      inner join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id     
      inner join @tblTechSkill tts on tts.InterviewId = im.id    
      left join @tblPatchCount tpc on tpc.InterviewId=ic.InterviewId    
      where ic.MeetingStatus=4 and (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and ul.EmailId=@Emailid 
	  and ((irf.IsQCRequired=1 and QCF.QCStatus=1) or irf.IsQCRequired=0)
	  and (ic.MoveToCP is null or ic.MoveToCP =0)     
      and irf.IsDeleted=0 and im.ClientHrContact=@Emailid   
      and cast(interviewDate as date) >= cast (@startDate as date)    
      and cast(interviewDate as date) <= cast (@endDate as date)    
      group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,          
      im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,          
      im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,          
      im.currentEmployerName, im.QCInterviewer, im.IsEmergency,  im.JoiningBiometryScore,           
      ca.Phone, ca.ResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage  , QCF.InterviewId ,ic.CandidateAvailable,tpc.MeetingPatchCount,tts.TechSkillScore, ic.ThumbnailTimeSeconds)a )    
    
   End    
END
ELSE
BEGIN
  if(@searchValue is not null)    
    
   Begin    
    set @totalResults=(select count(a.id) from (select im.id from InterviewMeetings im     
      inner join [dbo].[InterviewComments] ic on im.id = ic.InterviewId          
      inner join UserList ulc on ulc.EmailId = im.candidateEmail          
      inner join CandidatesMaster cm on ulc.id = cm.UserId          
      left join QCFeedbackReport QCF on im.id=QCF.InterviewId          
      inner join IRF on im.IRFId = IRF.IRFId              
      inner join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id          
      inner join dbo.UserList ul on ul.id = tc.ClientUserId          
      inner join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id      
      inner join @tblTechSkill tts on tts.InterviewId = im.id    
      left join @tblPatchCount tpc on tpc.InterviewId=ic.InterviewId    
      where ic.MeetingStatus=4 and (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and ul.EmailId=@Emailid 
	  and ((irf.IsQCRequired=1 and QCF.QCStatus=1) or irf.IsQCRequired=0)
	  and (ic.MoveToCP is null or ic.MoveToCP =0) and (im.candidateName like ('%' +@searchValue +'%') or irf.JDTiltle like ('%' +@searchValue +'%'))    
      --and ((irf.IsDeleted = 1 and cast(interviewDate as date) >= cast(DATEADD(day, -30, @time) as date)) or IRF.IsDeleted=0)     
      and irf.IsDeleted=0    
      and cast(interviewDate as date) >= cast (@startDate as date)    
      and cast(interviewDate as date) <= cast (@endDate as date)    
          
      group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,          
      im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,          
      im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,          
      im.currentEmployerName, im.QCInterviewer, im.IsEmergency,  im.JoiningBiometryScore,           
      ca.Phone, ca.ResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage  , QCF.InterviewId ,ic.CandidateAvailable,tpc.MeetingPatchCount,tts.TechSkillScore, ic.ThumbnailTimeSeconds) a)    
    
   End    
  Else    
   Begin    
    set @totalResults=(select count(a.id) from (select im.id from InterviewMeetings im     
      inner join [dbo].[InterviewComments] ic on im.id = ic.InterviewId          
      inner join UserList ulc on ulc.EmailId = im.candidateEmail          
      inner join CandidatesMaster cm on ulc.id = cm.UserId          
      left join QCFeedbackReport QCF on im.id=QCF.InterviewId          
      inner join IRF on im.IRFId = IRF.IRFId              
      inner join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id          
      inner join dbo.UserList ul on ul.id = tc.ClientUserId          
      inner join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id     
      inner join @tblTechSkill tts on tts.InterviewId = im.id    
      left join @tblPatchCount tpc on tpc.InterviewId=ic.InterviewId    
      where ic.MeetingStatus=4 and (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and ul.EmailId=@Emailid 
	  and ((irf.IsQCRequired=1 and QCF.QCStatus=1) or irf.IsQCRequired=0)
	  and (ic.MoveToCP is null or ic.MoveToCP =0)     
      --and ((irf.IsDeleted = 1 and cast(interviewDate as date) >= cast(DATEADD(day, -30, @time) as date)) or IRF.IsDeleted=0)       
      and irf.IsDeleted=0    
      and cast(interviewDate as date) >= cast (@startDate as date)    
      and cast(interviewDate as date) <= cast (@endDate as date)    
          
      group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,          
      im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,          
      im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,          
      im.currentEmployerName, im.QCInterviewer, im.IsEmergency,  im.JoiningBiometryScore,           
      ca.Phone, ca.ResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage  , QCF.InterviewId ,ic.CandidateAvailable,tpc.MeetingPatchCount,tts.TechSkillScore, ic.ThumbnailTimeSeconds)a )    
    
   End    
END
 End    
    
    
    
 select @totalResults    
End    
    
  
  

GO


