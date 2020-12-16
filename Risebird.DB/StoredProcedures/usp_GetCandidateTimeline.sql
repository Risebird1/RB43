/****** Object:  StoredProcedure [dbo].[usp_GetCandidateTimeline]    Script Date: 11-12-2020 05.54.02 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateTimeline'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateTimeline
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



Create PROCEDURE [dbo].[usp_GetCandidateTimeline]  
AS  
Begin
declare @InvitationSentId int, @AddedId int, @meetingStatusId int, @interviewStatusId int;
	set @InvitationSentId = (select id from CandidateStatusOptionsMst where StatusValue='Invitation Sent')
	set @AddedId = (select id from CandidateStatusOptionsMst where StatusValue='Added')
	set @meetingStatusId = (select id from CandidateStatusOptionsMst where StatusValue='Meeting Status')
	set @interviewStatusId = (select id from CandidateStatusOptionsMst where StatusValue='Interview Status')
	

	declare @invitationAttemptsTable table (CandidateMasterId int, IRFId int, InvitationDate datetime)
				insert into @invitationAttemptsTable 
				select CandidateId, IRFId, [Date] from CandidateStatus where [Status]=@InvitationSentId

	declare @addedOnTable table (CandidateMasterId int, IRFId int, OnboardingDate datetime)
				insert into @addedOnTable 
				select CandidateId, IRFId, [Date] from CandidateStatus where [Status]=@AddedId

	declare @meetingStatusTable table (CandidateMasterId int, IRFId int, [Date] datetime, Comments varchar(max))
				insert into @meetingStatusTable 
				select CandidateId, IRFId, [Date], Comments from CandidateStatus where [Status]=@meetingStatusId

	declare @interviewStatusTable table (CandidateMasterId int, IRFId int, [Date] datetime, Comments varchar(max))
				insert into @interviewStatusTable 
				select CandidateId, IRFId, [Date], Comments from CandidateStatus where [Status]=@interviewStatusId

	declare @currentStatusTable table (CandidateMasterId int, IRFId int, [Date] datetime, Comments varchar(max))
				insert into @currentStatusTable 
				select CandidateId, IRFId, [Date], Comments from CandidateStatus where id in (select max(id) from CandidateStatus group by IRFId, CandidateId)


		select ca.id as CandidateId, ca.CandidateMasterId, ul.EmailId, ul.FirstName as [Name], ca.ClientHRAttemptContact as HREmail, ca.IRFId, im.id as InterviewId,aot.OnboardingDate, iat.InvitationDate,
		mst.Comments as MeetingStatus, mst.[Date] as MeetingStatusDate, ist.Comments as InterviewStatus, ist.[Date] as InterviewStatusDate,
		cst.Comments as CurrentStatus, cst.[Date] as CurrentStatusDate, ic.InterviewStatus as CurrentInterviewStatus, qcfr.QCStatus,
		ic.MeetingStatus as CurrentMeetingStatus, im.confirmationStatus from Candidate ca 
		join CandidatesMaster cm on cm.id =ca.CandidateMasterId
		join UserList ul on ul.id =cm.UserId
		join IRF on IRF.id=ca.IRFId
		left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId =IRF.IRFId
		left join InterviewComments ic on ic.InterviewId = im.id
		left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id
		left join @addedOnTable aot on ca.CandidateMasterId=aot.CandidateMasterId and ca.IRFId=aot.IRFId
		left join @invitationAttemptsTable iat on ca.CandidateMasterId=iat.CandidateMasterId and ca.IRFId=iat.IRFId 
		left join @meetingStatusTable mst on ca.CandidateMasterId= mst.CandidateMasterId and ca.IRFId= mst.IRFId
		left join @interviewStatusTable ist on ca.CandidateMasterId= ist.CandidateMasterId and ca.IRFId= ist.IRFId
		left join @currentStatusTable cst on ca.CandidateMasterId= cst.CandidateMasterId and ca.IRFId= cst.IRFId --where InvitationDate is not null
	
				

End




GO


