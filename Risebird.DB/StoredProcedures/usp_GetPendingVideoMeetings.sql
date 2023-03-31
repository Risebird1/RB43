/****** Object:  StoredProcedure [dbo].[usp_GetAccounts]    Script Date: 24-03-2020 8.20.45 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPendingVideoMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPendingVideoMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_GetPendingVideoMeetings]  
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
where im.interviewDate<@timeEnd and im.interviewDate>@timeStart and ((ic.VideoLink is null or ic.VideoLink='') or ic.VideoDuration <=5) and im.ZoomMeetingId is not null and ISNULL(ca.IsProctor,0)!=1 
UNION
select distinct im.id as imid, im.ZoomMeetingId, im.interviewDate, im.zoomLink, irf.MeetingPlatform
from InterviewMeetings im 
join IRF on IRF.IRFId = im.IRFId
left join InterviewComments ic on ic.InterviewId=im.id
join UserList ul on ul.EmailId = im.candidateEmail
join CandidatesMaster cm on cm.UserId = ul.id
left join Candidate ca on (ca.CandidateMasterId = cm.id and ca.IRFId = IRF.id)
where im.interviewDate < (DATEADD(HOUR, -12, @timeEnd))and im.interviewDate>@timeStart and ((ic.VideoLink is null or ic.VideoLink='') or ic.VideoDuration <=5) and im.ZoomMeetingId is not null and (ca.IsProctor = 1)

End
GO


