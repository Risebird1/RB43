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
		
		declare @commonHRTable table (EmailId varchar(100),imid int, icid int, MeetingStatus int, InterviewStatus int, QCStatus int);
		insert into @commonHRTable 
		select EmailId,im.id, ic.id,ic.MeetingStatus, ic.InterviewStatus, qcf.QCStatus 
		from CandidatesMaster cm join UserList ul on ul.id=cm.UserId join InterviewMeetings im on im.candidateEmail=ul.EmailId 
		left join InterviewComments ic on ic.InterviewId=im.id
		left join QCFeedbackReport qcf on qcf.InterviewId = im.id
		where confirmationStatus=1 and ClientHrContact=@emailId

		select
		(select count(distinct(EmailId)) from @commonHRTable where InterviewStatus=1) as Proxy,
		(select count(distinct(EmailId)) from @commonHRTable where InterviewStatus=2 and QCStatus=1) as Selected,
		(select count(distinct(EmailId)) from @commonHRTable where InterviewStatus=3 or (InterviewStatus=2 and QCStatus=2)) as Rejected,
		(select count(distinct(EmailId)) from @commonHRTable where InterviewStatus=2 and QCStatus is null) as QCPending,
		(select count(distinct(EmailId)) from @commonHRTable where InterviewStatus=4) as BorderLine,
		(select count(distinct(EmailId)) from @commonHRTable where InterviewStatus is null and MeetingStatus=1) as NoShowCount,
		(select count(distinct(EmailId)) from @commonHRTable where InterviewStatus is null and (MeetingStatus=2 or MeetingStatus is null)) as ScheduledCount,
		(select count(distinct(EmailId)) from @commonHRTable where InterviewStatus is null and MeetingStatus=3) as DroppedCount,
		(select count(distinct(EmailId)) from @commonHRTable where InterviewStatus is null and MeetingStatus=4) as Pending

	End
else if(@roleId=4)
	Begin
		
		declare @commonStaffingTable table (EmailId varchar(100),imid int, icid int, MeetingStatus int, InterviewStatus int, QCStatus int);
		insert into @commonStaffingTable 
		select EmailId,im.id, ic.id,ic.MeetingStatus, ic.InterviewStatus, qcf.QCStatus 
		from CandidatesMaster cm join UserList ul on ul.id=cm.UserId join InterviewMeetings im on im.candidateEmail=ul.EmailId 
		left join InterviewComments ic on ic.InterviewId=im.id
		left join QCFeedbackReport qcf on qcf.InterviewId = im.id
		where confirmationStatus=1 and staffingAgency=@emailId

		select
		(select count(distinct(EmailId)) from @commonStaffingTable where InterviewStatus=1) as Proxy,
		(select count(distinct(EmailId)) from @commonStaffingTable where InterviewStatus=2 and QCStatus=1) as Selected,
		(select count(distinct(EmailId)) from @commonStaffingTable where InterviewStatus=3 or (InterviewStatus=2 and QCStatus=2)) as Rejected,
		(select count(distinct(EmailId)) from @commonStaffingTable where InterviewStatus=2 and QCStatus is null) as QCPending,
		(select count(distinct(EmailId)) from @commonStaffingTable where InterviewStatus=4) as BorderLine,
		(select count(distinct(EmailId)) from @commonStaffingTable where InterviewStatus is null and MeetingStatus=1) as NoShowCount,
		(select count(distinct(EmailId)) from @commonStaffingTable where InterviewStatus is null and (MeetingStatus=2 or MeetingStatus is null)) as ScheduledCount,
		(select count(distinct(EmailId)) from @commonStaffingTable where InterviewStatus is null and MeetingStatus=3) as DroppedCount,
		(select count(distinct(EmailId)) from @commonStaffingTable where InterviewStatus is null and MeetingStatus=4) as Pending

	End
	
else
	Begin

		declare @commonTable table (EmailId varchar(100),imid int, icid int, MeetingStatus int, InterviewStatus int, QCStatus int);
			insert into @commonTable 
			select EmailId,im.id, ic.id,ic.MeetingStatus, ic.InterviewStatus , qcf.QCStatus
			from TransClientUser tcu join irf on tcu.IRFId = irf.id join Candidate ca on ca.IRFId=irf.id
			join CandidatesMaster cm on cm.id = ca.CandidateMasterId join UserList ul on ul.id=cm.UserId join InterviewMeetings im on im.candidateEmail=ul.EmailId 
			left join InterviewComments ic on ic.InterviewId=im.id
			left join QCFeedbackReport qcf on qcf.InterviewId = im.id
			where confirmationStatus=1 and tcu.ClientUserId=@userId


			select
			(select count(distinct(EmailId)) from @commonTable where InterviewStatus=1) as Proxy,
			(select count(distinct(EmailId)) from @commonTable where InterviewStatus=2 and QCStatus=1) as Selected,
			(select count(distinct(EmailId)) from @commonTable where InterviewStatus=3 or (InterviewStatus=2 and QCStatus=2)) as Rejected,
			(select count(distinct(EmailId)) from @commonTable where InterviewStatus=2 and QCStatus is null) as QCPending,
			(select count(distinct(EmailId)) from @commonTable where InterviewStatus=4) as BorderLine,
			(select count(distinct(EmailId)) from @commonTable where InterviewStatus is null and MeetingStatus=1) as NoShowCount,
			(select count(distinct(EmailId)) from @commonTable where InterviewStatus is null and (MeetingStatus=2 or MeetingStatus is null)) as ScheduledCount,
			(select count(distinct(EmailId)) from @commonTable where InterviewStatus is null and MeetingStatus=3) as DroppedCount,
			(select count(distinct(EmailId)) from @commonTable where InterviewStatus is null and MeetingStatus=4) as Pending

		
	End


End
GO


