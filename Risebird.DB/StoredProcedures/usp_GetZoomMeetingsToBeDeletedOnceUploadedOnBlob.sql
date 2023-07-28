/****** Object:  StoredProcedure [dbo].[usp_GetZoomMeetingsToBeDeletedOnceUploadedOnBlob]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetZoomMeetingsToBeDeletedOnceUploadedOnBlob'))
BEGIN
    DROP PROCEDURE dbo.usp_GetZoomMeetingsToBeDeletedOnceUploadedOnBlob
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetZoomMeetingsToBeDeletedOnceUploadedOnBlob]  
@timeStart datetime,
@timeEnd datetime
AS  
Begin 

select distinct im.id as imid, im.ZoomMeetingId, im.interviewDate, im.zoomLink, irf.MeetingPlatform
from InterviewMeetings im 
join IRF on IRF.IRFId = im.IRFId
left join InterviewComments ic on ic.InterviewId=im.id
join UserList ul on ul.EmailId = im.candidateEmail
join CandidatesMaster cm on cm.UserId = ul.id
left join Candidate ca on (ca.CandidateMasterId = cm.id and ca.IRFId = IRF.id)
where im.interviewDate<=@timeEnd and im.interviewDate>=@timeStart and (ic.VideoLink is not null and ic.VideoLink !='' and ic.VideoDuration > 10 and ic.AudioTranscriptFileLink is not null and ic.AudioTranscriptFileLink != '') and im.ZoomMeetingId is not null and ISNULL(ca.IsProctor,0) != 1 
UNION
select distinct im.id as imid, im.ZoomMeetingId, im.interviewDate, im.zoomLink, irf.MeetingPlatform
from InterviewMeetings im 
join IRF on IRF.IRFId = im.IRFId
left join InterviewComments ic on ic.InterviewId=im.id
join UserList ul on ul.EmailId = im.candidateEmail
join CandidatesMaster cm on cm.UserId = ul.id
left join Candidate ca on (ca.CandidateMasterId = cm.id and ca.IRFId = IRF.id)
where im.interviewDate <= (DATEADD(HOUR, -12, @timeEnd))and im.interviewDate>=@timeStart and (ic.VideoLink is not null and ic.VideoLink !='' and ic.VideoDuration > 10 and ic.AudioTranscriptFileLink is not null and ic.AudioTranscriptFileLink != '') and im.ZoomMeetingId is not null and (ca.IsProctor = 1)

End
GO


