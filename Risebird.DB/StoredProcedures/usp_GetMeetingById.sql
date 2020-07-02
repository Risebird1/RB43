/****** Object:  StoredProcedure [dbo].[usp_GetMeetingById]    Script Date: 11-09-2019 8.42.44 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetMeetingById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetMeetingById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetMeetingById]

@interviewId int

AS
Begin

SELECT [InterviewMeetings].id as imid
     ,[InterviewMeetings].IRFId
	 ,interviewDate
	 ,interviewSlot
	 ,interviewDuration
	 ,interviewDuration
	 ,candidateEmail
	 ,ClientHrContact
	 ,ca.ResumeLink as resumeLink
	 ,confirmationStatus
	 ,zoomLink
	 ,[InterviewMeetings].risebirdHR
	 ,interviewer
	 ,PreferredLocation
	 ,ExpectedPerHourRate
	 ,staffingAgency
	 ,JobLevel
	 ,candidateName
	 ,joiningTime
	 ,currentEmployerName
	 ,ul.MobileNumber
	 ,JoiningBiometryScore
	 ,InterviewBiometryScore
	 ,InterviewComments.ApprovalComments
	 ,InterviewComments.FinalComments
	 ,InterviewComments.IsApproved
	 ,InterviewComments.IsSelected
	 ,IRF.id as irfIntId
	 ,IRF.IsBiometricEnabled
FROM [dbo].[InterviewMeetings]
inner join 
IRF on [InterviewMeetings].IRFId = IRF.IRFId
left join InterviewComments on InterviewMeetings.id=InterviewComments.InterviewId
left join UserList ul on ul.EmailId = InterviewMeetings.candidateEmail
join CandidatesMaster cm on cm.UserId = ul.id
join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id
WHERE
      [InterviewMeetings].id = @interviewId

	  End


GO


