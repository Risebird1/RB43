/****** Object:  StoredProcedure [dbo].[usp_GetClientCandidatesCount]    Script Date: 10-08-2020 6.18.17 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientCandidatesCount'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientCandidatesCount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetClientCandidatesCount]  
@emailId varchar(100)
AS  

Begin 

declare @profileId int, @roleId int, @userId int;
	set @profileId = (select id from [Profile] where [Name]='Client')
	set @userId  = (select id from UserList where EmailId=@emailId and idProfile=@profileId)
	set @roleId  = (select RoleId from ClientUser where UserId=@userId)

if(@roleId=1)
	Begin
		
		declare @commonHRTable table (EmailId varchar(100),imid int, icid int, MeetingStatus int, InterviewStatus int, QCStatus int, confirmationStatus int);
		insert into @commonHRTable 
		select EmailId,im.id, ic.id,ic.MeetingStatus, ic.InterviewStatus, qcf.QCStatus, im.confirmationStatus 
		from CandidatesMaster cm join UserList ul on ul.id=cm.UserId left join InterviewMeetings im on im.candidateEmail=ul.EmailId 
		join IRF on im.IRFId=IRF.IRFId 
		join Candidate ca on ca.IRFId=irf.id
		left join InterviewComments ic on ic.InterviewId=im.id
		left join QCFeedbackReport qcf on qcf.InterviewId = im.id
		where ClientHrContact=@emailId

		select
		(select count(distinct(EmailId)) from @commonHRTable where InterviewStatus=1 and confirmationStatus=1) as Proxy,
		(select count(distinct(EmailId)) from @commonHRTable where confirmationStatus=1 and  MeetingStatus = 4 and InterviewStatus=2) as Selected,
		(select count(distinct(EmailId)) from @commonHRTable where confirmationStatus=1 and MeetingStatus = 4 and (InterviewStatus=3 or InterviewStatus=2)) as Rejected,
		(select count(distinct(EmailId)) from @commonHRTable where confirmationStatus=1 and (((InterviewStatus=2 or InterviewStatus=4)) or (InterviewStatus is null and MeetingStatus=4))) as QCPending,
		(select count(distinct(EmailId)) from @commonHRTable where confirmationStatus=1 and MeetingStatus = 4 and InterviewStatus=4) as BorderLine,
		(select count(distinct(EmailId)) from @commonHRTable where confirmationStatus=1 and InterviewStatus is null and MeetingStatus=1) as NoShowCount,
		(select count(distinct(EmailId)) from @commonHRTable where  confirmationStatus=1 and InterviewStatus is null and (MeetingStatus=2 or MeetingStatus is null)) as ScheduledCount,
		(select count(distinct(EmailId)) from @commonHRTable where confirmationStatus=1 and InterviewStatus is null and MeetingStatus=3) as DroppedCount,
		(select count(distinct(EmailId)) from @commonHRTable) as TotalCandidates

	End
else if(@roleId=4)
	Begin
		
		declare @commonStaffingTable table (EmailId varchar(100),imid int, icid int, MeetingStatus int, InterviewStatus int, QCStatus int, confirmationStatus int);
		insert into @commonStaffingTable 
		select EmailId,im.id, ic.id,ic.MeetingStatus, ic.InterviewStatus, qcf.QCStatus , im.confirmationStatus
		from CandidatesMaster cm join UserList ul on ul.id=cm.UserId left join InterviewMeetings im on im.candidateEmail=ul.EmailId 
		join IRF on im.IRFId=IRF.IRFId 
		join Candidate ca on ca.IRFId=irf.id
		left join InterviewComments ic on ic.InterviewId=im.id
		left join QCFeedbackReport qcf on qcf.InterviewId = im.id
		where staffingAgency=@emailId

		select
		(select count(distinct(EmailId)) from @commonStaffingTable where InterviewStatus=1 and confirmationStatus=1) as Proxy,
		(select count(distinct(EmailId)) from @commonStaffingTable where confirmationStatus=1 and  MeetingStatus = 4 and InterviewStatus=2 ) as Selected,
		(select count(distinct(EmailId)) from @commonStaffingTable where confirmationStatus=1 and MeetingStatus = 4 and (InterviewStatus=3 or (InterviewStatus=2))) as Rejected,
		(select count(distinct(EmailId)) from @commonStaffingTable where  confirmationStatus=1 and (((InterviewStatus=2 or InterviewStatus=4)  and QCStatus is null) or (InterviewStatus is null and MeetingStatus=4))) as QCPending,
		(select count(distinct(EmailId)) from @commonStaffingTable where confirmationStatus=1 and MeetingStatus = 4 and InterviewStatus=4) as BorderLine,
		(select count(distinct(EmailId)) from @commonStaffingTable where confirmationStatus=1 and InterviewStatus is null and MeetingStatus=1) as NoShowCount,
		(select count(distinct(EmailId)) from @commonStaffingTable where confirmationStatus=1 and InterviewStatus is null and (MeetingStatus=2 or MeetingStatus is null)) as ScheduledCount,
		(select count(distinct(EmailId)) from @commonStaffingTable where confirmationStatus=1 and InterviewStatus is null and MeetingStatus=3) as DroppedCount,
		(select count(distinct(EmailId)) from @commonStaffingTable) as TotalCandidates

	End
	
else
	Begin

		declare @commonTable table (EmailId varchar(100),imid int, icid int, MeetingStatus int, InterviewStatus int, QCStatus int, confirmationStatus int);
			insert into @commonTable 
			select EmailId,im.id, ic.id,ic.MeetingStatus, ic.InterviewStatus , qcf.QCStatus, im.confirmationStatus
			from TransClientUser tcu join irf on tcu.IRFId = irf.id join Candidate ca on ca.IRFId=irf.id
			join CandidatesMaster cm on cm.id = ca.CandidateMasterId join UserList ul on ul.id=cm.UserId left join InterviewMeetings im on im.candidateEmail=ul.EmailId 
			left join InterviewComments ic on ic.InterviewId=im.id
			left join QCFeedbackReport qcf on qcf.InterviewId = im.id
			where tcu.ClientUserId=@userId


			select
			(select count(distinct(EmailId)) from @commonTable where InterviewStatus=1 and confirmationStatus=1) as Proxy,
			(select count(distinct(EmailId)) from @commonTable where  confirmationStatus=1 and  MeetingStatus = 4 and InterviewStatus=2) as Selected,
			(select count(distinct(EmailId)) from @commonTable where confirmationStatus=1 and MeetingStatus = 4 and InterviewStatus=3) as Rejected,
			(select count(distinct(EmailId)) from @commonTable where confirmationStatus=1 and (((InterviewStatus=2 or InterviewStatus=4)  and QCStatus is null) or (InterviewStatus is null and MeetingStatus=4))) as QCPending,
			(select count(distinct(EmailId)) from @commonTable where confirmationStatus=1 and MeetingStatus = 4  and InterviewStatus=4) as BorderLine,
			(select count(distinct(EmailId)) from @commonTable where confirmationStatus=1 and InterviewStatus is null and MeetingStatus=1) as NoShowCount,
			(select count(distinct(EmailId)) from @commonTable where confirmationStatus=1 and InterviewStatus is null and (MeetingStatus=2 or MeetingStatus is null)) as ScheduledCount,
			(select count(distinct(EmailId)) from @commonTable where confirmationStatus=1 and InterviewStatus is null and MeetingStatus=3) as DroppedCount,
			(select count(distinct(EmailId)) from @commonTable) as TotalCandidates

		
	End


End
GO


