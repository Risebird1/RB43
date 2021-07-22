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
	 ,ul.EmailId as candidateEmail
	 --,candidateEmail
	 ,ClientHrContact
	 ,ca.ResumeLink as resumeLink
	 ,Isnull(ca.EncodedResumeLink,'') as encodedresumeLink
	 ,confirmationStatus
	 ,zoomLink
	 ,[InterviewMeetings].risebirdHR
	 ,interviewer
	 ,PreferredLocation
	 ,ExpectedPerHourRate
	 ,staffingAgency
	 ,JobLevel,
	 case 
	 when ul.LastName is null then ul.FirstName
	 else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))
	 end as candidateName
	 --,candidateName
	 ,joiningTime
	 ,currentEmployerName
	 ,ca.Phone as MobileNumber,
	 ca.TotalExperience
	 ,JoiningBiometryScore
	 ,InterviewBiometryScore
	 ,InterviewComments.ApprovalComments
	 ,InterviewComments.FinalComments
	 ,InterviewComments.IsApproved
	 ,InterviewComments.IsSelected
	 ,IRF.id as irfIntId
	 ,IRF.IsBiometricEnabled
	 ,HRCoordinatorEmailId
	 ,QCInterviewer
	 ,InterviewComments.ThumbnailTimeSeconds
	 ,InterviewComments.VideoDuration
	 ,ulh.EmailId as ChannelOwner
	 ,IsEmergency
	 ,InterviewComments.VideoLink
FROM [dbo].[InterviewMeetings]
inner join 
IRF on [InterviewMeetings].IRFId = IRF.IRFId
left join InterviewComments on InterviewMeetings.id=InterviewComments.InterviewId
left join UserList ul on ul.EmailId = InterviewMeetings.candidateEmail
join CandidatesMaster cm on cm.UserId = ul.id
join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id
join UserList ulh on irf.RisebirdHr = ulh.id
WHERE
      [InterviewMeetings].id = @interviewId

End


GO


