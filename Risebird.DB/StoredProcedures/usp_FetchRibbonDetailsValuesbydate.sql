IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchRibbonDetailsValuesbydate'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchRibbonDetailsValuesbydate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE procedure usp_FetchRibbonDetailsValuesbydate  
@actionstatus varchar(30),  
@actiontype int,  
@emailid varchar(200),  
@currentdate datetime  
AS  
BEGIN  
Declare @Profile varchar(150)   
Select @Profile=pr.Name from UserList ul join Profile pr on pr.id=ul.idProfile where EmailId=@emailid  
 If(@actionstatus ='InterviewCount')  
 begin  
  if (@actiontype = 0)  
  begin  
   if (@Profile = 'SeniorHR' or @Profile = 'HR')  
   begin  
	select distinct *,IRF.Id as irfIntId  from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
	join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid 
	where confirmationStatus=1 and interviewer is null and ulsh.Emailid= @Emailid
   end  
   else  
   begin  
    select distinct *,IRF.Id as irfIntId from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id where confirmationStatus=1 and interviewer is null
   end  
  end  
  else  
  begin  
   select distinct im.*,irf.*,cl.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
   join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId   
   where confirmationStatus=1 and interviewer is null and ul.EmailId=@emailID   
   union   
   select distinct im.*,irf.*,cl.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join Userlist ul on ul.id=irf.RisebirdHr   
   where confirmationStatus=1 and interviewer is null and ul.EmailId=@emailID  
  end  
 end  
 else If(@actionstatus ='MeetingStatus')  
 begin  
  if (@actiontype = 0)  
  begin  
   if (@Profile = 'SeniorHR' or @Profile = 'HR')  
   begin  
    select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
    join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid   
    left join InterviewComments ic on ic.InterviewId= im.id   
    where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and ulsh.Emailid= @Emailid
   end  
   else  
   begin  
    select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
    left join InterviewComments ic on ic.InterviewId= im.id   
    where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) 
   end  
  end  
  else  
  begin  
   select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
   left join InterviewComments ic on ic.InterviewId= im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId   
   where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and ul.EmailId=@emailID
   union   
   select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
   left join InterviewComments ic on ic.InterviewId= im.id join Userlist ul on ul.id=irf.RisebirdHr   
   where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and ul.Emailid =@emailID
  end  
 end  
 else If(@actionstatus ='UnAssignedCoordianter')  
 begin  
  if (@Profile = 'SeniorHR' or @Profile = 'HR')  
  begin  
   select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
   join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id   
   where confirmationStatus=1 and isAutoCancelled=1 and ulsh.Emailid= @Emailid  
  end  
  else  
  begin  
   select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
   left join InterviewComments ic on ic.InterviewId=im.id   
   where confirmationStatus=1 and isAutoCancelled=1
  end  
 end  
 else If(@actionstatus ='ZoomLink')  
 begin  
  if (@actiontype = 0)  
  begin  
   if (@Profile = 'SeniorHR' or @Profile = 'HR')  
   begin  
    select  distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 and (zoomLink is null or zoomLink='') and ulsh.Emailid= @Emailid
   end  
   else  
   begin  
    select  distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 and (zoomLink is null or zoomLink='')  
   end  
  end  
  else  
  begin  
   select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId where confirmationStatus=1 and (zoomLink is null or zoomLink='') and ul.EmailId=@emailID
   union   
   select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr where confirmationStatus=1 and (zoomLink is null or zoomLink='') and ul.EmailId=@emailID
  end  
 end  
 else If(@actionstatus ='UnAttendedZoomLink')  
 begin  
  if (@actiontype = 0)  
  begin  
   if (@Profile = 'SeniorHR' or @Profile = 'HR')  
   begin  
	Select distinct im.*,irf.*,cl.Name,ic.*,ul.Emailid as HRCoordinatorEmailId,uli.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.WhoJoined from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id left join Userlist ul on ul.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist uli on uli.Emailid=im.interviewer join TransZoomMeetingExtendDetails WhoJoined on WhoJoined.Meetingid=im.ZoomMeetingId and ulsh.Emailid= @Emailid
   end  
   else  
   begin  
    Select distinct im.*,irf.*,cl.Name,ic.*,ul.Emailid as HRCoordinatorEmailId,uli.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.WhoJoined from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id left join Userlist ul on ul.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist uli on uli.Emailid=im.interviewer join TransZoomMeetingExtendDetails WhoJoined on WhoJoined.Meetingid=im.ZoomMeetingId
   end  
  end  
  else  
  begin  
   select distinct im.*,irf.*,cl.*,ic.*,uli.Emailid as HRCoordinatorEmailId,ulin.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id left join Userlist uli on uli.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist ulin on ulin.EmailId=im.interviewer Outer Apply (select distinct STUFF((SELECT distinct ', ' + ISNULL(ul.EmailId,t3.ParticipantName) from (Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and Meetingid=im.ZoomMeetingId )t3 left join InterviewerProfile ip on ip.NickName=t3.ParticipantName left join USERList ul on ul.id=ip.UserId FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'')WhoJoined from ZoomMeetingTransmission t where t.Meetingid=im.ZoomMeetingId) WhoJoined where confirmationStatus=1 and uli.EmailId =@emailID and im.ZoomMeetingId in (Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io' group by meetingid) as t1 left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io' group by meetingid) as t2 on t1.Meetingid=t2.Meetingid)
  end  
 end  
 else If(@actionstatus ='VideoLink')  
 begin  
  if (@actiontype = 0)  
  begin  
   if (@Profile = 'SeniorHR' or @Profile = 'HR')  
   begin  
    Select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and ulsh.Emailid= @Emailid  
   end  
   else  
   begin  
    Select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='')
   end  
  end  
  else  
  begin  
   select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and ul.EmailId=@emailID 
   union   
   select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and ul.EmailId=@emailID 
  end  
 end  
 else If(@actionstatus ='QCPendingReport')  
 begin  
  if (@actiontype = 0)  
  begin  
   if (@Profile = 'SeniorHR' or @Profile = 'HR')  
   begin  
	Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and ulsh.Emailid= @Emailid
   end  
   else  
   begin  
    Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null
   end  
  end  
  else  
  begin  
   select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and ul.EmailId=@emailID 
   union 
   select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and ul.EmailId=@emailID
  end  
 end  
 else If(@actionstatus ='QCReport')  
 begin  
  if (@actiontype = 0)  
  begin  
   if (@Profile = 'SeniorHR' or @Profile = 'HR')  
   begin  
    Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and ulsh.Emailid= @Emailid
   end  
   else  
   begin  
    Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8)
   end  
  end  
  else  
  begin  
   select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and ul.EmailId=@emailID 
   union 
   select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and ul.EmailId=@emailID
  end  
 end  
 
END
GO