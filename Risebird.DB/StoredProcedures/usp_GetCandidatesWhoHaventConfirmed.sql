IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidatesWhoHaventConfirmed'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidatesWhoHaventConfirmed
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetCandidatesWhoHaventConfirmed]  
@timeNow datetime
As
Begin

	select im.id as imid, im.candidateEmail, im.candidateName, im.IRFId, im.interviewDate, im.interviewer, im.confirmationStatus,
	irf.id as irfIntId, im.ZoomMeetingId, cl.[Name] as Client, ul.MobileNumber, ic.MeetingStatus, ic.InterviewStatus, 
	im.ClientHrContact, ulr.EmailId as ChannelOwner, IRF.CalendlyLink,
	SUBSTRING(IRF.CalendlyLink , LEN(IRF.CalendlyLink) -  CHARINDEX('=',REVERSE(IRF.CalendlyLink))+2  ,
	LEN(IRF.CalendlyLink)) as scheduleUniqueid, c.id as CandidateId, c.ClientHRAttemptContact  
	from InterviewMeetings im
	join IRF on irf.irfid = im.irfid
	join Client cl on cl.id = irf.ClientId
	left join InterviewComments ic on ic.InterviewId = im.id
	join UserList ul on ul.EmailId = im.candidateEmail
	join UserList ulr on ulr.id = irf.RisebirdHr
	left join UserList ulc on ulc.Emailid=im.CandidateEmail
	left join CandidatesMaster cm on cm.userid=ulc.id
	left join Candidate c on c.IRFId=IRF.id and c.CandidateMasterId=cm.id
	where cast(interviewDate as date) >= cast(@timeNow as date) and  cast(interviewDate as date) <= cast(DATEADD(day, 7, @timeNow) as date)
	and (MeetingStatus = 2 or MeetingStatus is null)
	and (CandidateResponse = 0 or CandidateResponse is null)
	and confirmationStatus = 1
	order by interviewDate desc

End
GO