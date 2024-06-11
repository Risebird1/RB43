IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchRibbonDetailsValuesbydate'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchRibbonDetailsValuesbydate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create procedure usp_FetchRibbonDetailsValuesbydate  
@actionstatus varchar(30),  
@actiontype int,  
@emailid varchar(200),  
@currentdate datetime,
@StartDate datetime,
@EndDate datetime
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
		select distinct im.*, ul.EmailId as ChannelOwner,IRF.Id as irfIntId,IRF.JDTiltle,cl.Name, 
				IRF.CalendlyLink,SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  ,
				LEN(IRF.CalendlyLink)) as scheduleUniqueid, im.id as imid, irf.AssessmentGroupId, asg.AssessmentGroup from InterviewMeetings im 
				join IRF on im.IRFId=irf.IRFId 
				join Client cl on irf.ClientId=cl.id 
				join SeniorHr sh on sh.Clientid=irf.ClientId 
				left join UserList ulsh on ulsh.id=sh.Userid 
				join UserList ul on ul.id= irf.RisebirdHr
				join AssessmentGroup asg on asg.id = irf.AssessmentGroupId
				where confirmationStatus=1 and interviewer is null and ulsh.Emailid= @Emailid and interviewDate between @StartDate and @EndDate
   end  
   else  
   begin  
    select distinct im.*, ul.EmailId as ChannelOwner,IRF.Id as irfIntId,IRF.JDTiltle,cl.Name,IRF.CalendlyLink,
				SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  ,
				LEN(IRF.CalendlyLink)) as scheduleUniqueid, im.id as imid, irf.AssessmentGroupId, asg.AssessmentGroup 
				from InterviewMeetings im 
				join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				join UserList ul on ul.id=  irf.RisebirdHr
				join AssessmentGroup asg on asg.id = irf.AssessmentGroupId
				where confirmationStatus=1 and interviewer is null and interviewDate between @StartDate and @EndDate
   end  
  end  
  else  
  begin  
   select distinct im.*,irf.*,cl.*, ulc.EmailId as ChannelOwner,IRF.Id as irfIntId ,IRF.JDTiltle,cl.Name,
			IRF.CalendlyLink,SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  , 
			LEN(IRF.CalendlyLink)) as scheduleUniqueid,im.id as imid, asg.AssessmentGroup
			from InterviewMeetings im join IRF on im.IRFId=irf.IRFId 
			join Client cl on irf.ClientId=cl.id 
			join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId 
			join UserList ulc on ulc.id=  irf.RisebirdHr
			join AssessmentGroup asg on asg.id = irf.AssessmentGroupId
   where confirmationStatus=1 and interviewer is null and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate  
   union   
   select distinct im.*,irf.*,cl.*, ul.EmailId as ChannelOwner,IRF.Id as irfIntId,IRF.JDTiltle,cl.Name, 
			IRF.CalendlyLink, SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  ,
			LEN(IRF.CalendlyLink)) as scheduleUniqueid,im.id as imid, asg.AssessmentGroup  from InterviewMeetings im
			join IRF on im.IRFId=irf.IRFId 
			join Client cl on irf.ClientId=cl.id join Userlist ul on ul.id=irf.RisebirdHr 
			join UserList ulc on ulc.id=  irf.RisebirdHr
			join AssessmentGroup asg on asg.id = irf.AssessmentGroupId
   where confirmationStatus=1 and interviewer is null and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate
  end  
 end  
 else If(@actionstatus ='MeetingStatus')  
begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'HR')
			begin
				select distinct *,uli.MobileNumber as InterviewerMobileNumber,ulc.EmailId as ChannelOwner,IRF.Id as irfIntId,im.id as imid from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid 
				left join InterviewComments ic on ic.InterviewId= im.id 
				left join UserList uli on uli.EmailId=im.interviewer
				left join UserList ulc on ulc.id=IRF.RisebirdHr
				where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and interviewDate between @StartDate and @EndDate and ulsh.Emailid= @Emailid and interviewdate<@currentdate
			end
			else
			begin
				select distinct *,uli.MobileNumber as InterviewerMobileNumber,ulc.EmailId as ChannelOwner,IRF.Id as irfIntId,im.id as imid from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				left join InterviewComments ic on ic.InterviewId= im.id 
				left join UserList uli on uli.EmailId=im.interviewer
				left join UserList ulc on ulc.id=IRF.RisebirdHr
				where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and interviewDate between @StartDate and @EndDate and interviewdate<@currentdate
			end
		end
		else
		begin
			select distinct im.*,irf.*,cl.*,ic.*,uli.MobileNumber as InterviewerMobileNumber,ulc.EmailId as ChannelOwner,IRF.Id as irfIntId,im.id as imid from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
			left join InterviewComments ic on ic.InterviewId= im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId 
			left join UserList uli on uli.EmailId=im.interviewer left join UserList ulc on ulc.id=IRF.RisebirdHr
			where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and interviewDate between @StartDate and @EndDate and ul.EmailId=@emailID and interviewdate<@currentdate
			union 
			select distinct im.*,irf.*,cl.*,ic.*,uli.MobileNumber as InterviewerMobileNumber,ulc.EmailId as ChannelOwner,IRF.Id as irfIntId,im.id as imid from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
			left join InterviewComments ic on ic.InterviewId= im.id join Userlist ul on ul.id=irf.RisebirdHr 
			left join UserList uli on uli.EmailId=im.interviewer left join UserList ulc on ulc.id=IRF.RisebirdHr
			where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and interviewDate between @StartDate and @EndDate and ul.Emailid =@emailID and interviewdate<@currentdate
		end
	end
 else If(@actionstatus ='UnAssignedCoordianter')  
 begin  
  if (@Profile = 'SeniorHR' or @Profile = 'HR')  
  begin  
   select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
   join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id   
   where confirmationStatus=1 and isAutoCancelled=1 and ulsh.Emailid= @Emailid and interviewDate between @StartDate and @EndDate  
  end  
  else  
  begin  
   select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
   left join InterviewComments ic on ic.InterviewId=im.id   
   where confirmationStatus=1 and isAutoCancelled=1 and interviewDate between @StartDate and @EndDate
  end  
 end  
 else If(@actionstatus ='ZoomLink')  
 begin  
  if (@actiontype = 0)  
  begin  
   if (@Profile = 'SeniorHR' or @Profile = 'HR')  
   begin  
    select  distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 and (zoomLink is null or zoomLink='') and ulsh.Emailid= @Emailid and interviewDate between @StartDate and @EndDate
   end  
   else  
   begin  
    select  distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 and (zoomLink is null or zoomLink='') and interviewDate between @StartDate and @EndDate
   end  
  end  
  else  
  begin  
   select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId where confirmationStatus=1 and (zoomLink is null or zoomLink='') and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate
   union   
   select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr where confirmationStatus=1 and (zoomLink is null or zoomLink='') and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate
  end  
 end  
 else If(@actionstatus ='UnAttendedZoomLink')  
 begin  
  if (@actiontype = 0)  
  begin  
   if (@Profile = 'SeniorHR' or @Profile = 'HR')  
   begin  
	Select distinct im.*,irf.*,cl.Name,ic.*,ul.Emailid as HRCoordinatorEmailId,uli.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.WhoJoined from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id left join Userlist ul on ul.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist uli on uli.Emailid=im.interviewer join TransZoomMeetingExtendDetails WhoJoined on WhoJoined.Meetingid=im.ZoomMeetingId and ulsh.Emailid= @Emailid and interviewDate between @StartDate and @EndDate
   end  
   else  
   begin  
    Select distinct im.*,irf.*,cl.Name,ic.*,ul.Emailid as HRCoordinatorEmailId,uli.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.WhoJoined from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id left join Userlist ul on ul.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist uli on uli.Emailid=im.interviewer join TransZoomMeetingExtendDetails WhoJoined on WhoJoined.Meetingid=im.ZoomMeetingId and interviewDate between @StartDate and @EndDate
   end  
  end  
  else  
  begin  
   select distinct im.*,irf.*,cl.*,ic.*,uli.Emailid as HRCoordinatorEmailId,ulin.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id left join Userlist uli on uli.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist ulin on ulin.EmailId=im.interviewer Outer Apply (select distinct STUFF((SELECT distinct ', ' + ISNULL(ul.EmailId,t3.ParticipantName) from (Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and Meetingid=im.ZoomMeetingId )t3 left join InterviewerProfile ip on ip.NickName=t3.ParticipantName left join USERList ul on ul.id=ip.UserId FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'')WhoJoined from ZoomMeetingTransmission t where t.Meetingid=im.ZoomMeetingId) WhoJoined where confirmationStatus=1 and uli.EmailId =@emailID and im.ZoomMeetingId in (Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io' group by meetingid) as t1 left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io' group by meetingid) as t2 on t1.Meetingid=t2.Meetingid) and interviewDate between @StartDate and @EndDate
  end  
 end  
 else If(@actionstatus ='VideoLink')  
 begin  
  if (@actiontype = 0)  
  begin  
   if (@Profile = 'SeniorHR' or @Profile = 'HR')  
   begin  
    Select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and ulsh.Emailid= @Emailid and interviewDate between @StartDate and @EndDate
   end  
   else  
   begin  
    Select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and interviewDate between @StartDate and @EndDate
   end  
  end  
  else  
  begin  
   select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate
   union   
   select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate
  end  
 end  
 else If(@actionstatus ='QCPendingReport')  
 begin  
  if (@actiontype = 0)  
  begin  
   if (@Profile = 'SeniorHR')  
   begin  
	Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,uli.MobileNumber as InterviewerMobileNumber from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and ulsh.Emailid= @Emailid
   end  
   else  
   begin  
    Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,IRF.id as irfIntId,uli.MobileNumber as InterviewerMobileNumber from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1 where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and ISNULL(im.RedoInterview,0)=0
   and interviewDate between @StartDate and @EndDate
   end  
  end  
  else  
  begin  
   select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,IRF.id as irfIntId,uli.MobileNumber as InterviewerMobileNumber  from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1 where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and ISNULL(im.RedoInterview,0)=0 and ul.EmailId=@emailID 
   and interviewDate between @StartDate and @EndDate
   union 
   select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,IRF.id as irfIntId,uli.MobileNumber as InterviewerMobileNumber  from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1 where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and ISNULL(im.RedoInterview,0)=0 and ul.EmailId=@emailID
  and interviewDate between @StartDate and @EndDate
  end  
 end  
 else If(@actionstatus ='QCReport')  
 begin  
  if (@actiontype = 0)  
  begin  
   if (@Profile = 'SeniorHR')  
   begin  
    Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and ulsh.Emailid= @Emailid
   end  
   else  
   begin  
    Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,tq.PaymentComplianceCount as Redflags,IRF.id as irfIntId from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1  
	where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and interviewDate between @StartDate and @EndDate
   end  
  end  
  else  
  begin  
   select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,tq.PaymentComplianceCount as Redflags,IRF.id as irfIntId from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1 where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate
   union 
   select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,tq.PaymentComplianceCount as Redflags,IRF.id as irfIntId from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1 where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate
  end  
 end  


 If(@actionstatus ='ReScheduleCount')  
 begin  
  if (@actiontype = 0)  
  begin  
   if (@Profile = 'SeniorHR' or @Profile = 'HR')  
   begin  
	select distinct *,IRF.Id as irfIntId  from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
	join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid 
	where confirmationStatus=2 and ulsh.Emailid= @Emailid and interviewDate between @StartDate and @EndDate
   end  
   else  
   begin  
    select distinct *,IRF.Id as irfIntId from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id where confirmationStatus=2 and interviewDate between @StartDate and @EndDate
   end  
  end  
  else  
  begin  
   select distinct im.*,irf.*,cl.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
   join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId   
   where confirmationStatus=2 and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate   
   union   
   select distinct im.*,irf.*,cl.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join Userlist ul on ul.id=irf.RisebirdHr   
   where confirmationStatus=2 and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate
  end  
 end

 	else If(@actionstatus ='PotentialRiskByClient')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'HR')
			begin
				select distinct ccfr.Comment,ccfr.Interviewid,im.*, cl.Name,ul.EmailId as ChannelOwner,IRF.id as irfIntId ,ccfr.Rating,ccfr.CreatedAt as RateAddedOn, ccfr.CreatedBy as RateAddedBy,ic.InterviewStatus,tq.Redflags,ic.MeetingStatus
				 ,ps.IsApproved,itp.RedMarker
				 from  ClientCommentsForReport ccfr 
				 join InterviewMeetings im on ccfr.InterviewId=im.id
				 join InterviewComments ic on ic.InterviewId=im.id 
			    left join Trans_QCFormReport tq on tq.Interviewid=im.id
				join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid 
				join UserList ul on ul.id=  irf.RisebirdHr
				left join PaymentSLADetails ps on im.id=ps.Interviewid
				left join InterviewerProfile itp on itp.UserId = ul.id
				where ccfr.Rating<=3 and ulsh.Emailid= @Emailid and interviewDate between @StartDate and @EndDate
			end
			else
			begin
				select distinct ccfr.Comment,ccfr.Interviewid,im.*, cl.Name,ul.EmailId as ChannelOwner, IRF.id as irfIntId,ccfr.Rating,ccfr.CreatedAt as RateAddedOn, ccfr.CreatedBy as RateAddedBy
				,ic.InterviewStatus,tq.Redflags,ic.MeetingStatus,ps.PaymentComplianceCount,ps.IsApproved,itp.RedMarker
				from  ClientCommentsForReport ccfr 
				join InterviewMeetings im on ccfr.InterviewId=im.id
				join InterviewComments ic on ic.InterviewId=im.id 
			    left join Trans_QCFormReport tq on tq.Interviewid=im.id 
				left join PaymentSLADetails ps on ps.Interviewid=im.id
				join IRF on im.IRFId=irf.IRFId  join Client cl on irf.ClientId=cl.id 
				join UserList ul on ul.id=  irf.RisebirdHr
				left join InterviewerProfile itp on itp.UserId = ul.id
				where  ccfr.Rating<=3 and interviewDate between @StartDate and @EndDate
			end
		end
		else
		begin
			select distinct ccfr.Comment,ccfr.Interviewid,im.*,cl.Name,ulc.EmailId as ChannelOwner,IRF.id as irfIntId,ccfr.Rating,ccfr.CreatedAt as RateAddedOn,ccfr.CreatedBy as RateAddedBy,ic.InterviewStatus,tq.Redflags,ic.MeetingStatus, ps.PaymentComplianceCount,ps.IsApproved
			,itp.RedMarker
			from  ClientCommentsForReport ccfr 
			join InterviewMeetings im on ccfr.InterviewId=im.id
			join InterviewComments ic on ic.InterviewId=im.id 
			left join PaymentSLADetails ps on ps.Interviewid=im.id
			left join Trans_QCFormReport tq on tq.Interviewid=im.id
			join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
			join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId
			join Userlist ul on ul.id=HR.UserId 
			join UserList ulc on ulc.id=  irf.RisebirdHr
			left join InterviewerProfile itp on itp.UserId = ul.id
			where ccfr.Rating<=3 and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate
			union 
			select distinct ccfr.Comment,ccfr.Interviewid,im.*,cl.Name,ul.EmailId as ChannelOwner,IRF.id as irfIntId,ccfr.Rating,ccfr.CreatedAt as RateAddedOn,ccfr.CreatedBy as RateAddedBy,ic.InterviewStatus,tq.Redflags,ic.MeetingStatus,ps.PaymentComplianceCount,ps.IsApproved
			,itp.RedMarker
			from  ClientCommentsForReport ccfr 
				join InterviewMeetings im on ccfr.InterviewId=im.id
				join InterviewComments ic on ic.InterviewId=im.id 
				left join PaymentSLADetails ps on ps.Interviewid=im.id
			    left join Trans_QCFormReport tq on tq.Interviewid=im.id
				join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				join Userlist ul on ul.id=irf.RisebirdHr 
				left join InterviewerProfile itp on itp.UserId = ul.id
			where ccfr.Rating<=3 and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate
		end
	end

		else If(@actionstatus ='NeedClarity')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'HR')
			begin
				Select distinct im.*,im.id as InterviewId,cl.Name,ul.EmailId as ChannelOwner from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join UserList ul on ul.id=  irf.RisebirdHr where confirmationStatus=1 and CandidateResponse=4 and interviewdate>=@currentdate and ulsh.Emailid= @Emailid and interviewDate between @StartDate and @EndDate
			end
			else
			begin
				Select distinct im.*,im.id as InterviewId,cl.Name,ul.EmailId as ChannelOwner from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join UserList ul on ul.id=  irf.RisebirdHr where confirmationStatus=1 and CandidateResponse=4 and interviewdate>=@currentdate and interviewDate between @StartDate and @EndDate
			end
		end
		else
		begin
			select distinct im.*,irf.*,cl.*,ic.*,uli.EmailId as ChannelOwner from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId left join UserList uli on uli.id=  irf.RisebirdHr where confirmationStatus=1 and CandidateResponse=4 and interviewdate>=@currentdate  and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate
			union 
			select distinct im.*,irf.*,cl.*,ic.*,ul.EmailId as ChannelOwner from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr where confirmationStatus=1 and CandidateResponse=4 and interviewdate>=@currentdate and ul.EmailId=@emailID and interviewDate between @StartDate and @EndDate
		end
	end
		else If(@actionstatus ='CancelledInterviewCount')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'HR')
			begin
				select distinct im.*, cl.Name as Client,ul.EmailId as BillingOwner,IRF.id as irfIntId from InterviewMeetings im 
				join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid 
				join UserList ul on ul.id=  irf.RisebirdHr 
				where  confirmationStatus=2 and  interviewDate between @StartDate and @EndDate and ul.EmailId=@emailID and im.RedoInterview=0
			end
			else
			begin
				select distinct im.*, cl.Name as Client,ul.EmailId as BillingOwner,IRF.id as irfIntId from InterviewMeetings im 
				join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				join UserList ul on ul.id=  irf.RisebirdHr
				where confirmationStatus=2 and  interviewDate between @StartDate and @EndDate and im.RedoInterview=0
			end
		end
		else
		begin
			select distinct im.*,cl.Name as Client,ul.EmailId as BillingOwner,IRF.id as irfIntId  from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
			join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId 
			join UserList ulc on ulc.id=  irf.RisebirdHr
			where confirmationStatus=2 and  interviewDate between @StartDate and @EndDate and ul.EmailId=@emailID and im.RedoInterview=0
			union 
			select distinct im.*,cl.Name as Client,ul.EmailId as BillingOwner,IRF.id as irfIntId  from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join Userlist ul on ul.id=irf.RisebirdHr 
			join UserList ulc on ulc.id=  irf.RisebirdHr
			where confirmationStatus=2 and  interviewDate between @StartDate and @EndDate and ul.EmailId=@emailID and im.RedoInterview=0
		end
	end

END
GO