/****** Object:  StoredProcedure [dbo].[usp_GetPendingTranscriptMeetings]    Script Date: 24-03-2020 8.20.45 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPendingTranscriptMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPendingTranscriptMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetPendingTranscriptMeetings]  -- where video links are present but transcript links are missing
@timeStart datetime,
@timeEnd datetime
AS  
Begin 

select distinct im.id as imid, im.ZoomMeetingId, im.interviewDate, im.zoomLink, irf.MeetingPlatform, 
ic.VideoDuration, irf.ClientId, cl.[Name] as ClientCompanyName,im.MeetingPlatForm as InterviewMeetingPlatForm
from InterviewMeetings im 
join IRF on IRF.IRFId = im.IRFId
join Client cl on cl.id = irf.ClientId
left join InterviewComments ic on ic.InterviewId=im.id
join UserList ul on ul.EmailId = im.candidateEmail
join CandidatesMaster cm on cm.UserId = ul.id
left join Candidate ca on (ca.CandidateMasterId = cm.id and ca.IRFId = IRF.id)
where im.interviewDate<=@timeEnd and im.interviewDate>=@timeStart and (ic.VideoLink is not null and (ic.AudioTranscriptFileLink is null or ic.AudioTranscriptFileLink ='')) and im.ZoomMeetingId is not null and ISNULL(ca.IsProctor,0) != 1 --or (ic.AudioTranscriptFileLink is null or ic.AudioTranscriptFileLink = '')
UNION
select distinct im.id as imid, im.ZoomMeetingId, im.interviewDate, im.zoomLink, irf.MeetingPlatform, 
ic.VideoDuration, irf.ClientId, cl.[Name] as ClientCompanyName,im.MeetingPlatForm as InterviewMeetingPlatForm
from InterviewMeetings im 
join IRF on IRF.IRFId = im.IRFId
join Client cl on cl.id = irf.ClientId
left join InterviewComments ic on ic.InterviewId=im.id
join UserList ul on ul.EmailId = im.candidateEmail
join CandidatesMaster cm on cm.UserId = ul.id
left join Candidate ca on (ca.CandidateMasterId = cm.id and ca.IRFId = IRF.id)
where im.interviewDate <= (DATEADD(HOUR, -12, @timeEnd))and im.interviewDate>=@timeStart and (ic.VideoLink is not null and (ic.AudioTranscriptFileLink is null or ic.AudioTranscriptFileLink ='')) and im.ZoomMeetingId is not null and (ca.IsProctor = 1)--or (ic.AudioTranscriptFileLink is null or ic.AudioTranscriptFileLink = '')

End
GO


