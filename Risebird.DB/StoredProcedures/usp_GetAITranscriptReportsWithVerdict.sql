IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAITranscriptReportsWithVerdict'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAITranscriptReportsWithVerdict
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetAITranscriptReportsWithVerdict]  
@timeNow datetime,
@startDate datetime = null,
@endDate datetime = null
As
Begin

 
	Select distinct im.id as InterviewId, IRF.id as irfIntId, im.interviewDate,IRF.IRFId,
	im.candidateName, im.candidateEmail, im.interviewer, ul.MobileNumber as InterviewerMobileNumber,
	uli.EmailId as ChannelOwner, ic.InterviewStatus,
	im.confirmationStatus, ic.MeetingStatus, ic.AudioTranscriptFileLink, 
	ic.IsAITranscriptAnalysisAttempted, ic.IsAITranscriptAnalysisFailed,
	ic.AITranscriptVerdict, ic.AIVerdict as AIQCVerdict
	from InterviewMeetings im 
	join IRF on IRF.IRFId=im.IRFId
	join [dbo].[InterviewComments] ic on im.id = ic.InterviewId
	left join UserList ul on ul.EmailId=im.interviewer
	join UserList uli on uli.id= irf.RisebirdHr
	where  ic.AITranscriptVerdict is not  null and --ic.AITranscriptVerdict in ('yes') and--ic.AIVerdict in ('No')
	((@startDate is not null and @endDate is not null and  cast(@startDate as date) <= cast(im.interviewDate as date) and cast(im.interviewDate as date) <= cast (@endDate as date)) or
	(@startDate is null and @endDate is null) or
	(@startDate is null and @endDate is  not null and cast(im.interviewDate as date) <= cast (@endDate as date)) or
	(@startDate is not null and @endDate is null and cast(@startDate as date) <= cast (im.interviewDate  as date)))

End
GO

