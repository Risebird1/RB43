IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAIQCNegativeVerdictReports'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAIQCNegativeVerdictReports
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetAIQCNegativeVerdictReports]  
@timeNow datetime
As
Begin

 
	Select distinct im.id as InterviewId, IRF.id as irfIntId, im.interviewDate,IRF.IRFId,
	im.candidateName, im.candidateEmail, im.interviewer, ul.MobileNumber as InterviewerMobileNumber,
	uli.EmailId as ChannelOwner, ic.InterviewStatus,
	im.confirmationStatus, ic.MeetingStatus, ic.AudioTranscriptFileLink, ic.IsAITranscriptAnalysisAttempted, ic.IsAITranscriptAnalysisFailed
	from InterviewMeetings im 
	join IRF on IRF.IRFId=im.IRFId
	join [dbo].[InterviewComments] ic on im.id = ic.InterviewId
	left join UserList ul on ul.EmailId=im.interviewer
	join UserList uli on uli.id= irf.RisebirdHr
	where  ic.AIVerdict in ('No')
	and cast(im.interviewDate as date) >= cast(dateadd(day, -30, @timeNow) as date)
	order by im.interviewDate desc

End
GO

