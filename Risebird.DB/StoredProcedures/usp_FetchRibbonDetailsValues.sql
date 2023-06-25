IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchRibbonDetailsValues'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchRibbonDetailsValues
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create procedure usp_FetchRibbonDetailsValues
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
			if (@Profile = 'SeniorHR' or @Profile = 'SupportAdmin')
			begin
				select distinct * ,IRF.Id as irfIntId from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid 
				where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, GETDATE())<1 and ulsh.Emailid= @Emailid
			end
			else
			begin
				select distinct *,IRF.Id as irfIntId from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, GETDATE())<1
			end
		end
		else
		begin
			select distinct im.*,irf.*,cl.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
			join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId 
			where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, GETDATE())<1 and ul.EmailId=@emailID 
			union 
			select distinct im.*,irf.*,cl.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join Userlist ul on ul.id=irf.RisebirdHr 
			where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, GETDATE())<1 and ul.EmailId=@emailID
		end
	end
	else If(@actionstatus ='MeetingStatus')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'SupportAdmin')
			begin
				select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid 
				left join InterviewComments ic on ic.InterviewId= im.id 
				where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7 and ulsh.Emailid= @Emailid and interviewdate<@currentdate
			end
			else
			begin
				select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				left join InterviewComments ic on ic.InterviewId= im.id 
				where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7 and interviewdate<@currentdate
			end
		end
		else
		begin
			select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
			left join InterviewComments ic on ic.InterviewId= im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId 
			where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7 and ul.EmailId=@emailID and interviewdate<@currentdate
			union 
			select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
			left join InterviewComments ic on ic.InterviewId= im.id join Userlist ul on ul.id=irf.RisebirdHr 
			where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7 and ul.Emailid =@emailID and interviewdate<@currentdate
		end
	end
	else If(@actionstatus ='UnAssignedCoordianter')
	begin
		if (@Profile = 'SeniorHR' or @Profile = 'SupportAdmin')
		begin
			select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
			join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id 
			where confirmationStatus=1 and isAutoCancelled=1 and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ulsh.Emailid= @Emailid
		end
		else
		begin
			select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
			left join InterviewComments ic on ic.InterviewId=im.id 
			where confirmationStatus=1 and isAutoCancelled=1 and DATEDIFF(DAY, interviewDate, GETDATE())<7
		end
	end
	else If(@actionstatus ='ZoomLink')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'SupportAdmin')
			begin
				select  distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 and (zoomLink is null or zoomLink='') and ulsh.Emailid= @Emailid and interviewDate>@currentdate
			end
			else
			begin
				select  distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 and (zoomLink is null or zoomLink='') and interviewDate>@currentdate
			end
		end
		else
		begin
			select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId where confirmationStatus=1 and (zoomLink is null or zoomLink='') and ul.EmailId=@emailID  and interviewDate>@currentdate
			union 
			select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr where confirmationStatus=1 and (zoomLink is null or zoomLink='') and ul.EmailId=@emailID and interviewDate>@currentdate
		end
	end
	else If(@actionstatus ='UnAttendedZoomLink')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'SupportAdmin')
			begin
				Select distinct im.*,irf.*,cl.Name,ic.*,ul.Emailid as HRCoordinatorEmailId,uli.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id left join Userlist ul on ul.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist uli on uli.Emailid=im.interviewer Outer Apply (select distinct STUFF((SELECT distinct ', ' + ISNULL(ul.EmailId,t3.ParticipantName) from (Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and Meetingid=im.ZoomMeetingId )t3 LEFT join(Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_LEFT' and Meetingid=im.ZoomMeetingId )t4 on t3.ParticipantName =t4.ParticipantName left join InterviewerProfile ip on ip.NickName=t3.ParticipantName left join USERList ul on ul.id=ip.UserId where t4.id is null FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'')WhoJoined from ZoomMeetingTransmission t where t.Meetingid=im.ZoomMeetingId) WhoJoined where im.ZoomMeetingId in (Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io' group by meetingid) as t1 left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io' group by meetingid) as t2 on t1.Meetingid=t2.Meetingid where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate())) and ulsh.Emailid= @Emailid
			end
			else
			begin
				Select distinct im.*,irf.*,cl.Name,ic.*,ul.Emailid as HRCoordinatorEmailId,uli.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id left join Userlist ul on ul.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist uli on uli.Emailid=im.interviewer Outer Apply (select distinct STUFF((SELECT distinct ', ' + ISNULL(ul.EmailId,t3.ParticipantName) from (Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and Meetingid=im.ZoomMeetingId )t3 LEFT join(Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_LEFT' and Meetingid=im.ZoomMeetingId )t4 on t3.ParticipantName =t4.ParticipantName left join InterviewerProfile ip on ip.NickName=t3.ParticipantName left join USERList ul on ul.id=ip.UserId where t4.id is null FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'')WhoJoined from ZoomMeetingTransmission t where t.Meetingid=im.ZoomMeetingId) WhoJoined where im.ZoomMeetingId in (Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io' group by meetingid) as t1 left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io' group by meetingid) as t2 on t1.Meetingid=t2.Meetingid where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate()))
			end
		end
		else
		begin
			select distinct im.*,irf.*,cl.*,ic.*,uli.Emailid as HRCoordinatorEmailId,ulin.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.* from InterviewMeetings im   join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id left join Userlist uli on uli.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist ulin on ulin.EmailId=im.interviewer Outer Apply (select distinct STUFF((SELECT distinct ', ' + ISNULL(ul.EmailId,t3.ParticipantName) from (Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and Meetingid=im.ZoomMeetingId )t3 LEFT join(Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_LEFT' and Meetingid=im.ZoomMeetingId )t4 on t3.ParticipantName =t4.ParticipantName left join InterviewerProfile ip on ip.NickName=t3.ParticipantName left join USERList ul on ul.id=ip.UserId where t4.id is null FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'')WhoJoined from ZoomMeetingTransmission t where t.Meetingid=im.ZoomMeetingId) WhoJoined   where confirmationStatus=1 and uli.Emailid =@emailID and im.ZoomMeetingId in (Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io' group by meetingid) as t1 left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io' group by meetingid) as t2 on t1.Meetingid=t2.Meetingid where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,20,im.Interviewdate)>=dateadd(minute,330,getdate()))
		end
	end
	else If(@actionstatus ='VideoLink')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'SupportAdmin')
			begin
				Select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and DATEDIFF(DAY, im.interviewDate, GETDATE())<7 and ulsh.Emailid= @Emailid
			end
			else
			begin
				Select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and DATEDIFF(DAY, im.interviewDate, GETDATE())<7
			end
		end
		else
		begin
			select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and ul.EmailId=@emailID and DATEDIFF(DAY, im.interviewDate, GETDATE())<7 
			union 
			select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and ul.EmailId=@emailID and DATEDIFF(DAY, im.interviewDate, GETDATE())<7
		end
	end
	else If(@actionstatus ='UnProcessedResume')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'SupportAdmin')
			begin
				Select IRF.Id as irfIntId,IRF.IRFId,IRF.JDTiltle,IRF.PrimarySkillInvite as PrimarySkill,cl.Name as Client,count(cr.id) as TotalView from IRF join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join CandidateResume cr on cr.IRFId=IRF.id where ulsh.Emailid= @Emailid and ISNULL(cr.IsProcessed,0)=0 group by IRF.Id,IRF.IRFId,IRF.JDTiltle,IRF.PrimarySkillInvite,cl.Name
			end
			else
			begin
				select IRF.Id as irfIntId,IRF.IRFId,IRF.JDTiltle,IRF.PrimarySkillInvite as PrimarySkill,cl.Name as Client,count(cr.id) as TotalView from IRF join Client cl on cl.id=irf.ClientId join CandidateResume cr on cr.IRFId=IRF.id Where ISNULL(cr.IsProcessed,0)=0 group by IRF.Id,IRF.IRFId,IRF.JDTiltle,IRF.PrimarySkillInvite,cl.Name
			end
		end
		else
		begin
			Select CR.id as irfIntId,CR.IRFId,CR.JDTiltle,CR.Name as Client,Cr.PrimarySkillInvite as PrimarySkill,Count(CR.Candidateid) as TotalView from (select IRF.Id,IRF.IRFId,IRF.JDTiltle,IRF.PrimarySkillInvite,cl.Name,cr.id as Candidateid from  IRF join Client cl on irf.ClientId=cl.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId join CandidateResume cr on cr.IRFId=IRF.id where ul.EmailId=@emailID and ISNULL(cr.IsProcessed,0)=0 
			union 
			Select IRF.Id,IRF.IRFId,IRF.JDTiltle,IRF.PrimarySkillInvite,cl.Name,cr.id as Candidateid from IRF join Client cl on irf.ClientId=cl.id join Userlist ul on ul.id=irf.RisebirdHr join CandidateResume cr on cr.IRFId=IRF.id where ul.EmailId=@emailID and ISNULL(cr.IsProcessed,0)=0 ) as Cr Group By CR.id,CR.IRFId,CR.JDTiltle,CR.Name,Cr.PrimarySkillInvite
		end
	end
	else If(@actionstatus ='QCPendingReport')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'SupportAdmin')
			begin
				Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id left join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ulsh.Emailid= @Emailid
			end
			else
			begin
				Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id left join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7
			end
		end
		else
		begin
			select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id left join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ul.EmailId=@emailID 
			union 
			select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ul.EmailId=@emailID
		end
	end
	else If(@actionstatus ='QCReport')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'SupportAdmin')
			begin
				Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ulsh.Emailid= @Emailid
			end
			else
			begin
				Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7
			end
		end
		else
		begin
			select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ul.EmailId=@emailID 
			union 
			select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id  join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ul.EmailId=@emailID
		end
	end
	else If(@actionstatus ='NeedClarity')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'SupportAdmin')
			begin
				Select distinct im.*,im.id as InterviewId,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid where confirmationStatus=1 and CandidateResponse=4 and interviewdate>=@currentdate and ulsh.Emailid= @Emailid
			end
			else
			begin
				Select distinct im.*,im.id as InterviewId,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id where confirmationStatus=1 and CandidateResponse=4 and interviewdate>=@currentdate
			end
		end
		else
		begin
			select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId where confirmationStatus=1 and CandidateResponse=4 and interviewdate>=@currentdate  and ul.EmailId=@emailID 
			union 
			select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr where confirmationStatus=1 and CandidateResponse=4 and interviewdate>=@currentdate and ul.EmailId=@emailID 
		end
	end
END
GO