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
			if (@Profile = 'SeniorHR' or @Profile = 'HR')
			begin
				select distinct im.*, ul.EmailId as ChannelOwner,IRF.Id as irfIntId,IRF.JDTiltle, IRF.CalendlyLink,SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  , LEN(IRF.CalendlyLink)) as scheduleUniqueid, im.id as imid, irf.AssessmentGroupId from InterviewMeetings im 
				join IRF on im.IRFId=irf.IRFId 
				join Client cl on irf.ClientId=cl.id 
				join SeniorHr sh on sh.Clientid=irf.ClientId 
				left join UserList ulsh on ulsh.id=sh.Userid 
				join UserList ul on ul.id= irf.RisebirdHr 
				where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, GETDATE())<1 and ulsh.Emailid= @Emailid
			end
			else
			begin
				select distinct im.*, ul.EmailId as ChannelOwner,IRF.Id as irfIntId,IRF.JDTiltle,IRF.CalendlyLink,SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  , LEN(IRF.CalendlyLink)) as scheduleUniqueid, im.id as imid, irf.AssessmentGroupId from InterviewMeetings im 
				join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				join UserList ul on ul.id=  irf.RisebirdHr
				where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, GETDATE())<1
			end
		end
		else
		begin
			select distinct im.*,irf.*,cl.*, ulc.EmailId as ChannelOwner,IRF.Id as irfIntId ,IRF.JDTiltle,IRF.CalendlyLink,SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  , LEN(IRF.CalendlyLink)) as scheduleUniqueid,im.id as imid  from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
			join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId 
			join UserList ulc on ulc.id=  irf.RisebirdHr
			where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, GETDATE())<1 and ul.EmailId=@emailID 
			union 
			select distinct im.*,irf.*,cl.*, ul.EmailId as ChannelOwner,IRF.Id as irfIntId,IRF.JDTiltle, IRF.CalendlyLink, SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  , LEN(IRF.CalendlyLink)) as scheduleUniqueid,im.id as imid  from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join Userlist ul on ul.id=irf.RisebirdHr 
			join UserList ulc on ulc.id=  irf.RisebirdHr
			where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, GETDATE())<1 and ul.EmailId=@emailID
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
				where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7 and ulsh.Emailid= @Emailid and interviewdate<@currentdate
			end
			else
			begin
				select distinct *,uli.MobileNumber as InterviewerMobileNumber,ulc.EmailId as ChannelOwner,IRF.Id as irfIntId,im.id as imid from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				left join InterviewComments ic on ic.InterviewId= im.id 
				left join UserList uli on uli.EmailId=im.interviewer
				left join UserList ulc on ulc.id=IRF.RisebirdHr
				where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7 and interviewdate<@currentdate
			end
		end
		else
		begin
			select distinct im.*,irf.*,cl.*,ic.*,uli.MobileNumber as InterviewerMobileNumber,ulc.EmailId as ChannelOwner,IRF.Id as irfIntId,im.id as imid from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
			left join InterviewComments ic on ic.InterviewId= im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId 
			left join UserList uli on uli.EmailId=im.interviewer left join UserList ulc on ulc.id=IRF.RisebirdHr
			where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7 and ul.EmailId=@emailID and interviewdate<@currentdate
			union 
			select distinct im.*,irf.*,cl.*,ic.*,uli.MobileNumber as InterviewerMobileNumber,ulc.EmailId as ChannelOwner,IRF.Id as irfIntId,im.id as imid from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
			left join InterviewComments ic on ic.InterviewId= im.id join Userlist ul on ul.id=irf.RisebirdHr 
			left join UserList uli on uli.EmailId=im.interviewer left join UserList ulc on ulc.id=IRF.RisebirdHr
			where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7 and ul.Emailid =@emailID and interviewdate<@currentdate
		end
	end
	else If(@actionstatus ='UnAssignedCoordianter')
	begin
		if (@Profile = 'SeniorHR' or @Profile = 'HR')
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
	else If(@actionstatus ='InterviewerChurn')
	begin
		select distinct ipr.Id, ipr.UserId, ul.EmailId as InterviewerEmailId, ul.MobileNumber as InterviewerMobileNumber, 
		(ul.FirstName +' '+ ul.LastName) as InterviewerName, ul.WhatsappNumber as InterviewerWhatsappNumber, ipr.ResumeLink as InterviewerResume, 
		ipr.EmploymentComment, ipr.CompanyName, ipr.Isavailable, ipr.AvailabilityResponseDate, ipr.IsEmployed from InterviewerProfile ipr join UserList ul on ul.id = ipr.UserId
		where ipr.Isavailable in (3,4) order by ipr.AvailabilityResponseDate desc--and ul.IsActive = 1

	end
	else If(@actionstatus ='ZoomLink')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'HR')
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
			if (@Profile = 'SeniorHR' or @Profile = 'HR')
			begin
				Select distinct im.*,irf.*,cl.Name,ic.*,ul.Emailid as HRCoordinatorEmailId,uli.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.*, IRF.Id as irfIntId from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id left join Userlist ul on ul.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist uli on uli.Emailid=im.interviewer Outer Apply (select distinct STUFF((SELECT distinct ', ' + ISNULL(ul.EmailId,t3.ParticipantName) from (Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and Meetingid=im.ZoomMeetingId )t3 LEFT join(Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_LEFT' and Meetingid=im.ZoomMeetingId )t4 on t3.ParticipantName =t4.ParticipantName left join InterviewerProfile ip on ip.NickName=t3.ParticipantName left join USERList ul on ul.id=ip.UserId where t4.id is null FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'')WhoJoined from ZoomMeetingTransmission t where t.Meetingid=im.ZoomMeetingId) WhoJoined where im.ZoomMeetingId in (Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io' group by meetingid) as t1 left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io' group by meetingid) as t2 on t1.Meetingid=t2.Meetingid where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate())) and ulsh.Emailid= @Emailid
			end
			else
			begin
				Select distinct im.*,irf.*,cl.Name,ic.*,ul.Emailid as HRCoordinatorEmailId,uli.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.*, IRF.Id as irfIntId from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id left join Userlist ul on ul.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist uli on uli.Emailid=im.interviewer Outer Apply (select distinct STUFF((SELECT distinct ', ' + ISNULL(ul.EmailId,t3.ParticipantName) from (Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and Meetingid=im.ZoomMeetingId )t3 LEFT join(Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_LEFT' and Meetingid=im.ZoomMeetingId )t4 on t3.ParticipantName =t4.ParticipantName left join InterviewerProfile ip on ip.NickName=t3.ParticipantName left join USERList ul on ul.id=ip.UserId where t4.id is null FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'')WhoJoined from ZoomMeetingTransmission t where t.Meetingid=im.ZoomMeetingId) WhoJoined where im.ZoomMeetingId in (Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io' group by meetingid) as t1 left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io' group by meetingid) as t2 on t1.Meetingid=t2.Meetingid where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate()))
			end
		end
		else
		begin
			select distinct im.*,irf.*,cl.*,ic.*,uli.Emailid as HRCoordinatorEmailId,ulin.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.*, IRF.Id as irfIntId from InterviewMeetings im   join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id left join Userlist uli on uli.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist ulin on ulin.EmailId=im.interviewer Outer Apply (select distinct STUFF((SELECT distinct ', ' + ISNULL(ul.EmailId,t3.ParticipantName) from (Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and Meetingid=im.ZoomMeetingId )t3 LEFT join(Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_LEFT' and Meetingid=im.ZoomMeetingId )t4 on t3.ParticipantName =t4.ParticipantName left join InterviewerProfile ip on ip.NickName=t3.ParticipantName left join USERList ul on ul.id=ip.UserId where t4.id is null FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'')WhoJoined from ZoomMeetingTransmission t where t.Meetingid=im.ZoomMeetingId) WhoJoined   where confirmationStatus=1 and uli.Emailid =@emailID and im.ZoomMeetingId in (Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io' group by meetingid) as t1 left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io' group by meetingid) as t2 on t1.Meetingid=t2.Meetingid where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,20,im.Interviewdate)>=dateadd(minute,330,getdate()))
		end
	end
	else If(@actionstatus ='VideoLink')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'HR')
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
			if (@Profile = 'SeniorHR' or @Profile = 'HR')
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
			if (@Profile = 'SeniorHR' or @Profile = 'HR')
			begin
				Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,IRF.id as irfIntId,uli.MobileNumber as InterviewerMobileNumber,imjt.Joinedon from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id left join Trans_QCFormReport tq on tq.Interviewid=im.id left join InterviewMeetingsJoinTime imjt on im.interviewer=imjt.interviewer and im.id=imjt.interviewid where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ulsh.Emailid= @Emailid
			end
			else
			begin
				Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,IRF.id as irfIntId,uli.MobileNumber as InterviewerMobileNumber,imjt.Joinedon from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id left join Trans_QCFormReport tq on tq.Interviewid=im.id left join InterviewMeetingsJoinTime imjt on im.interviewer=imjt.interviewer and im.id=imjt.interviewid where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7
			end
		end
		else
		begin
			select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name ,im.id imid,IRF.id as irfIntId,uli.MobileNumber as InterviewerMobileNumber,imjt.Joinedon from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id left join Trans_QCFormReport tq on tq.Interviewid=im.id left join InterviewMeetingsJoinTime imjt on im.interviewer=imjt.interviewer and im.id=imjt.interviewid where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ul.EmailId=@emailID 
			union 
			select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,im.id imid,IRF.id as irfIntId,uli.MobileNumber as InterviewerMobileNumber,imjt.Joinedon from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id left join InterviewMeetingsJoinTime imjt on im.interviewer=imjt.interviewer and im.id=imjt.interviewid where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ul.EmailId=@emailID
		end
	end
	else If(@actionstatus ='QCReport')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'HR')
			begin
				Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags,im.id imid,IRF.id as irfIntId,imjt.Joinedon from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id join Trans_QCFormReport tq on tq.Interviewid=im.id left join InterviewMeetingsJoinTime imjt on im.interviewer=imjt.interviewer and im.id=imjt.interviewid where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ulsh.Emailid= @Emailid order by tq.Redflags desc
			end
			else
			begin
				Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags,im.id imid,IRF.id as irfIntId,imjt.Joinedon from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id join Trans_QCFormReport tq on tq.Interviewid=im.id left join InterviewMeetingsJoinTime imjt on im.interviewer=imjt.interviewer and im.id=imjt.interviewid where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 order by tq.Redflags desc
			end
		end
		else
		begin
			select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags,im.id imid,IRF.id as irfIntId,imjt.Joinedon from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id join Trans_QCFormReport tq on tq.Interviewid=im.id left join InterviewMeetingsJoinTime imjt on im.interviewer=imjt.interviewer and im.id=imjt.interviewid where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ul.EmailId=@emailID 
			union 
			select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags,im.id imid,IRF.id as irfIntId,imjt.Joinedon from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id  join Trans_QCFormReport tq on tq.Interviewid=im.id left join InterviewMeetingsJoinTime imjt on im.interviewer=imjt.interviewer and im.id=imjt.interviewid where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ul.EmailId=@emailID order by tq.Redflags desc
		end
	end
	else If(@actionstatus ='NeedClarity')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'HR')
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
	else If(@actionstatus ='ReScheduleCount')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'HR')
			begin
				select distinct im.*, 
				REPLACE(Cancellationsource,'Reschedule on','') as FutureSchduleDate,
				cl.Name as Name,
				ul.EmailId as ClientEmail
				,ul.EmailId as ChannelOwner,IRF.Id as irfIntId from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid 
				join UserList ul on ul.id=  irf.RisebirdHr 
				where confirmationStatus=2 and Cancellationsource like'%Reschedule%' and CAST(interviewDate as DATE) >=CAST(@currentdate as DATE) and ulsh.Emailid= @Emailid
			end
			else
			begin
				select distinct im.*,
				REPLACE(Cancellationsource,'Reschedule on','') as FutureSchduleDate,
				 cl.Name as Name,
				 ul.EmailId as ClientEmail,
				ul.EmailId as ChannelOwner,IRF.Id as irfIntId from InterviewMeetings im 
				join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				join UserList ul on ul.id=  irf.RisebirdHr
				where confirmationStatus=2 and Cancellationsource like'%Reschedule%' and CAST(interviewDate as DATE) >=CAST(@currentdate as DATE)
			end
		end
		else
		begin
			select distinct im.*,
			REPLACE(Cancellationsource,'Reschedule on','') as FutureSchduleDate,
			cl.Name as Name,
			ulc.EmailId as ClientEmail,
			irf.*,cl.*, ulc.EmailId as ChannelOwner,IRF.Id as irfIntId from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
			join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId 
			join UserList ulc on ulc.id=  irf.RisebirdHr
			where confirmationStatus=2 and Cancellationsource like'%Reschedule%' and CAST(interviewDate as DATE) >=CAST(@currentdate as DATE) and ul.EmailId=@emailID 
			union 
			select distinct im.*,
			REPLACE(Cancellationsource,'Reschedule on','') as FutureSchduleDate,
			cl.Name as Name,
			ulc.EmailId as ClientEmail,
			irf.*,cl.*, ul.EmailId as ChannelOwner,IRF.Id as irfIntId from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join Userlist ul on ul.id=irf.RisebirdHr 
			join UserList ulc on ulc.id=  irf.RisebirdHr
			where confirmationStatus=2 and Cancellationsource like'%Reschedule%' and CAST(interviewDate as DATE) >=CAST(@currentdate as DATE) and ul.EmailId=@emailID
		end
	end
	else If(@actionstatus ='PotentialRiskInterviewCount')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'HR')
			begin
				select distinct im.*, ul.EmailId as ChannelOwner,IRF.Id as irfIntId,
				cl.Name as Name,
				cr.Comment
				from
				 candidateRatings cr
				 inner join InterviewMeetings im on cr.interviewid=im.id
				 join IRF on im.IRFId=irf.IRFId
				join Client cl on irf.ClientId=cl.id 
				join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid 
				join UserList ul on ul.id=  irf.RisebirdHr 
				where cr.Question2<=3 and (Cast(interviewDate as Date)> Cast(DATEADD(day,-7,getdate()) as Date) and Cast(interviewDate as Date)<= getdate()) and ulsh.Emailid= @Emailid
			end
			else
			begin
				select distinct im.*, ul.EmailId as ChannelOwner,IRF.Id as irfIntId,
				cl.Name as Name,
				cr.Comment
				from
				candidateRatings cr
				inner join InterviewMeetings im  on cr.interviewid=im.id
				join IRF on im.IRFId=irf.IRFId
				join Client cl on irf.ClientId=cl.id 
				join UserList ul on ul.id=  irf.RisebirdHr
				where cr.Question2<=3 and (Cast(interviewDate as Date)> Cast(DATEADD(day,-7,getdate()) as Date) and Cast(interviewDate as Date)<= getdate())
			end
		end
		else
		begin
			select distinct im.*,irf.*,cl.*, ulc.EmailId as ChannelOwner,IRF.Id as irfIntId,
			cl.Name as Name,
			cr.Comment
			from candidateRatings cr
			inner join InterviewMeetings im on cr.interviewid=im.id
			 join IRF on im.IRFId=irf.IRFId
			
			join Client cl on irf.ClientId=cl.id 
			join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId 
			join UserList ulc on ulc.id=  irf.RisebirdHr
			where cr.Question2<=3 and (Cast(interviewDate as Date)> Cast(DATEADD(day,-7,getdate()) as Date) and Cast(interviewDate as Date)<= getdate()) and ul.EmailId=@emailID 
			union 
			select distinct im.*,irf.*,cl.*, ul.EmailId as ChannelOwner,IRF.Id as irfIntId,
			cl.Name as Name,
			cr.Comment
			from candidateRatings cr
			inner join InterviewMeetings im on cr.interviewid=im.id
			join IRF on im.IRFId=irf.IRFId 
			join Client cl on irf.ClientId=cl.id join Userlist ul on ul.id=irf.RisebirdHr 
			join UserList ulc on ulc.id=  irf.RisebirdHr
			where cr.Question2<=3 and (Cast(interviewDate as Date)> Cast(DATEADD(day,-7,getdate()) as Date) and Cast(interviewDate as Date)<= getdate()) and ul.EmailId=@emailID
		end
	end
	else If(@actionstatus ='SuspeciousInterviewerCount')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'HR')
			begin
				select (ul.FirstName +' '+ ul.LastName) as SuspiciousName, ul.EmailId as SuspiciousEmail,ip.CreatedOn as InterviewAddedOn,ip.CreatedBy ,ip.ResumeLink as ResumeLink,
				si.Comment as SuspiciousComment,si.CreatedBy as SuspiciousAddedBy,ul.MobileNumber as SuspiciousMobileNumber from SuspiciousInterviewer si Join UserList ul on si.UserId=ul.id join InterviewerProfile ip on ul.id=ip.UserId
				join SeniorHr sh on ul.id=sh.Userid
				where si.CreatedBy=@Emailid and si.SuspiciousStatus=1
			end
			else
			begin
				select (ul.FirstName +' '+ ul.LastName) as SuspiciousName,ul.EmailId as SuspiciousEmail,ip.CreatedOn as InterviewAddedOn,ip.CreatedBy,
				ip.ResumeLink as ResumeLink,si.Comment as SuspiciousComment,
				si.CreatedBy as SuspiciousAddedBy,ul.MobileNumber as SuspiciousMobileNumber from SuspiciousInterviewer si Join UserList ul on si.UserId=ul.id join InterviewerProfile ip on ul.id=ip.UserId
				where si.SuspiciousStatus=1
			end
		end
		else
		begin
			select (ul.FirstName +' '+ ul.LastName) as SuspiciousName,ul.EmailId as SuspiciousEmail,ip.CreatedOn as InterviewAddedOn,ip.CreatedBy,
			ip.ResumeLink as ResumeLink,si.Comment as SuspiciousComment,si.CreatedBy as SuspiciousAddedBy,ul.MobileNumber as SuspiciousMobileNumber 
			from SuspiciousInterviewer si Join UserList ul on si.UserId=ul.id join InterviewerProfile ip on ul.id=ip.UserId
				join SeniorHr sh on ul.id=sh.Userid
				where si.CreatedBy=@Emailid and si.SuspiciousStatus=1
			union 
			select (ul.FirstName +' '+ ul.LastName) as SuspiciousName,ul.EmailId as SuspiciousEmail,ip.CreatedOn as InterviewAddedOn,ip.CreatedBy,ip.ResumeLink as ResumeLink,
			si.Comment as SuspiciousComment,si.CreatedBy as SuspiciousAddedBy,ul.MobileNumber as SuspiciousMobileNumber from SuspiciousInterviewer si Join UserList ul on si.UserId=ul.id join InterviewerProfile ip on ul.id=ip.UserId
				join SeniorHr sh on ul.id=sh.Userid
				where si.CreatedBy=@Emailid and si.SuspiciousStatus=1
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
				where  confirmationStatus=2 and  CAST(interviewDate as DATE) >=CAST(@currentdate as DATE) and ul.EmailId=@emailID and im.RedoInterview=0
			end
			else
			begin
				select distinct im.*, cl.Name as Client,ul.EmailId as BillingOwner,IRF.id as irfIntId from InterviewMeetings im 
				join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				join UserList ul on ul.id=  irf.RisebirdHr
				where confirmationStatus=2 and  CAST(interviewDate as DATE) >=CAST(@currentdate as DATE) and im.RedoInterview=0
			end
		end
		else
		begin
			select distinct im.*,cl.Name as Client,ul.EmailId as BillingOwner,IRF.id as irfIntId  from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
			join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId 
			join UserList ulc on ulc.id=  irf.RisebirdHr
			where confirmationStatus=2 and  CAST(interviewDate as DATE) >=CAST(@currentdate as DATE) and ul.EmailId=@emailID and im.RedoInterview=0
			union 
			select distinct im.*,cl.Name as Client,ul.EmailId as BillingOwner,IRF.id as irfIntId  from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join Userlist ul on ul.id=irf.RisebirdHr 
			join UserList ulc on ulc.id=  irf.RisebirdHr
			where confirmationStatus=2 and  CAST(interviewDate as DATE) >=CAST(@currentdate as DATE) and ul.EmailId=@emailID and im.RedoInterview=0
		end
	end
    else If(@actionstatus ='RedoInterviewCount')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'HR')
			begin
				select distinct im.*, cl.Name as Client,ulc.EmailId as BillingOwner,IRF.id as irfIntId,
				IRF.JDTiltle,
				IRF.JDLink as JDLinkv,
				ca.ClientHRAttemptContact as CandidateClientHR,
				itp.CompanyName,
				itp.CodedResumeLink as encodedresumeLink,
				ca.ResumeLink as resumeLink,
				ca.ClientHRAttemptContact as CandidateClientHR,
				IRF.ClientName,
				ic.VideoLink,
				ic.VideoDuration,
				IRF.IsAadhaarVerificationRequired,
				Case When irf.IsNewSkillChannel=1 then pm.DurationSelectedMin else IRF.interviewDuration end as interviewDuration,
				ca.Phone as MobileNumber,
				case when ulc.LastName is null then ulc.FirstName 
				
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                
  end as candidateName,
  im.id imid,
  ulcho.EmailId as ChannelOwner,
  ulc.EmailId as candidateEmail
				from InterviewMeetings im                              
                inner join IRF on im.IRFId = IRF.IRFId                              
               left join UserList ulc on im.candidateEmail= ulc.EmailId 
			   left join UserList uli on im.interviewer= uli.EmailId
			   left join UserList ulcho on ulcho.id  = irf.RisebirdHr 
			   left join InterviewerProfile itp on itp.UserId = uli.id
			    left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId 
              join CandidatesMaster cm on ulc.id = cm.UserId                                             
                left join Client cl on cl.id = irf.ClientId                             
               join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId 
			   left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.PlanTypeEnumValue=IRF.Heuristicsscore and pm.CompanyType=IRF.CompanyType  
				where  ulc.EmailId=@emailID and im.RedoInterview=1 and (Cast(interviewDate as Date)> Cast(DATEADD(day,-7,getdate()) as Date) and Cast(interviewDate as Date)<= getdate())
			end
			else
			begin
				select distinct im.*, cl.Name as Client,ulc.EmailId as BillingOwner,IRF.id as irfIntId,
				IRF.JDTiltle,
				IRF.JDLink as JDLink,
			    ca.ClientHRAttemptContact as CandidateClientHR,
				itp.CompanyName,
				itp.CodedResumeLink as encodedresumeLink,
				ca.ResumeLink as resumeLink,
				ca.ClientHRAttemptContact as CandidateClientHR,
				IRF.ClientName,
				ic.VideoLink,
				ic.VideoDuration,
				IRF.IsAadhaarVerificationRequired,
				Case When irf.IsNewSkillChannel=1 then pm.DurationSelectedMin else IRF.interviewDuration end as interviewDuration,
				ca.Phone as MobileNumber,
				case when ulc.LastName is null then ulc.FirstName                
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                
  end as candidateName,
  im.id imid,
  ulcho.EmailId as ChannelOwner,
  ulc.EmailId as candidateEmail
				from InterviewMeetings im                              
  inner join IRF on im.IRFId = IRF.IRFId                              
  left join UserList ulc on im.candidateEmail= ulc.EmailId 
  left join UserList uli on im.interviewer= uli.EmailId
  left join UserList ulcho on ulcho.id  = irf.RisebirdHr 
			   left join InterviewerProfile itp on itp.UserId = uli.id
			    left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId 
  join CandidatesMaster cm on ulc.id = cm.UserId                                             
  left join Client cl on cl.id = irf.ClientId                             
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId 
  left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.PlanTypeEnumValue=IRF.Heuristicsscore and pm.CompanyType=IRF.CompanyType  
				where im.RedoInterview=1 and (Cast(interviewDate as Date)> Cast(DATEADD(day,-7,getdate()) as Date) and Cast(interviewDate as Date)<= getdate())
			end
		end
		else
		begin
			select distinct im.*,cl.Name as Client,ulc.EmailId as BillingOwner,IRF.id as irfIntId ,
			IRF.JDTiltle,
			IRF.JDLink as JDLin,
			ca.ClientHRAttemptContact as CandidateClientHR,
			itp.CompanyName,
			itp.CodedResumeLink as encodedresumeLink,
			ca.ResumeLink as resumeLink,
			ca.ClientHRAttemptContact as CandidateClientHR,
			IRF.ClientName,
			ic.VideoLink,
			ic.VideoDuration,
			IRF.IsAadhaarVerificationRequired,
			Case When irf.IsNewSkillChannel=1 then pm.DurationSelectedMin else IRF.interviewDuration end as interviewDuration,
			ca.Phone as MobileNumber,
			case when ulc.LastName is null then ulc.FirstName                
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                
  end as candidateName,
  im.id imid,
  ulcho.EmailId as ChannelOwner,
  ulc.EmailId as candidateEmail
			from InterviewMeetings im                              
  inner join IRF on im.IRFId = IRF.IRFId                              
  left join UserList ulc on im.candidateEmail= ulc.EmailId 
  left join UserList uli on im.interviewer= uli.EmailId
  left join UserList ulcho on ulcho.id  = irf.RisebirdHr 
			   left join InterviewerProfile itp on itp.UserId = uli.id
			    left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId 
  join CandidatesMaster cm on ulc.id = cm.UserId                                             
  left join Client cl on cl.id = irf.ClientId                             
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId 
  left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.PlanTypeEnumValue=IRF.Heuristicsscore and pm.CompanyType=IRF.CompanyType  
			where ulc.EmailId=@emailID and im.RedoInterview=1 and (Cast(interviewDate as Date)> Cast(DATEADD(day,-7,getdate()) as Date) and Cast(interviewDate as Date)<= getdate())
			union 
			select distinct im.*,cl.Name as Client,ulc.EmailId as BillingOwner,IRF.id as irfIntId,
			IRF.JDTiltle,
			IRF.JDLink as JDLink,
			ca.ClientHRAttemptContact as CandidateClientHR,
			itp.CompanyName,
			itp.CodedResumeLink as encodedresumeLink,
			ca.ResumeLink as resumeLink,
			ca.ClientHRAttemptContact as CandidateClientHR,
			IRF.ClientName,
			ic.VideoLink,
			ic.VideoDuration,
			IRF.IsAadhaarVerificationRequired,
			Case When irf.IsNewSkillChannel=1 then pm.DurationSelectedMin else IRF.interviewDuration end as interviewDuration,
			ca.Phone as MobileNumber,
			case when ulc.LastName is null then ulc.FirstName                
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                
  end as candidateName,
  im.id imid,
  ulcho.EmailId as ChannelOwner,
  ulc.EmailId as candidateEmail
			from InterviewMeetings im                              
  inner join IRF on im.IRFId = IRF.IRFId                              
  left join UserList ulc on im.candidateEmail= ulc.EmailId 
  left join UserList uli on im.interviewer= uli.EmailId
  left join UserList ulcho on ulcho.id  = irf.RisebirdHr 
left join InterviewerProfile itp on itp.UserId = uli.id
  join CandidatesMaster cm on ulc.id = cm.UserId 
   left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId 
  left join Client cl on cl.id = irf.ClientId                             
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId 
  left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.PlanTypeEnumValue=IRF.Heuristicsscore and pm.CompanyType=IRF.CompanyType 
			where ulc.EmailId=@emailID and im.RedoInterview=1 and (Cast(interviewDate as Date)> Cast(DATEADD(day,-7,getdate()) as Date) and Cast(interviewDate as Date)<= getdate())
		end
	end
	else If(@actionstatus ='PotentialRiskByClient')
	begin
		if (@actiontype = 0)
		begin
			if (@Profile = 'SeniorHR' or @Profile = 'HR')
			begin
				select distinct ccfr.Comment,im.*, cl.Name as Client,ul.EmailId as BillingOwner,IRF.id as irfIntId 
				 from  ClientCommentsForReport ccfr join InterviewMeetings im on ccfr.InterviewId=im.id
				join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid 
				join UserList ul on ul.id=  irf.RisebirdHr 
				where  interviewDate between dateAdd(DD,-7,getdate()) and getdate() and ccfr.Rating<=3 and ulsh.Emailid= @Emailid
			end
			else
			begin
				select distinct ccfr.Comment,im.*, IRF.id as irfIntId 
				from  ClientCommentsForReport ccfr 
				join InterviewMeetings im on ccfr.InterviewId=im.id
				join IRF on im.IRFId=irf.IRFId  
				where interviewDate between dateAdd(DD,-7,getdate()) and getdate() and ccfr.Rating<=3
			end
		end
		else
		begin
			select distinct ccfr.Comment,im.*,cl.Name as Client,ul.EmailId as BillingOwner,IRF.id as irfIntId  from  ClientCommentsForReport ccfr 
			join InterviewMeetings im on ccfr.InterviewId=im.id
			join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
			join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId
			join Userlist ul on ul.id=HR.UserId 
			join UserList ulc on ulc.id=  irf.RisebirdHr
			where ccfr.Rating<=3 and ul.EmailId=@emailID and interviewDate between dateAdd(DD,-7,getdate()) and getdate() and ccfr.Rating<=3
			union 
			select distinct ccfr.Comment,im.*,cl.Name as Client,ul.EmailId as BillingOwner,IRF.id as irfIntId  from  ClientCommentsForReport ccfr 
				join InterviewMeetings im on ccfr.InterviewId=im.id
				join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id 
				join Userlist ul on ul.id=irf.RisebirdHr 
			join UserList ulc on ulc.id=  irf.RisebirdHr
			where ccfr.Rating<=3 and ul.EmailId=@emailID and interviewDate between dateAdd(DD,-7,getdate()) and getdate() and ccfr.Rating<=3
		end
	end
END

GO