IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAIInterviewDetailsBySessionId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAIInterviewDetailsBySessionId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetAIInterviewDetailsBySessionId
@SessionId varchar(50)
AS
BEGIN

declare @irfId int 
set @irfId  = (select id from IRF where IRFId = (select IRFId from InterviewMeetings where SessionId =@SessionId))

select im.id as InterviewId, im.interviewDate, im.AssistantId, im.candidateEmail, ulc.MobileNumber, im.interviewer,
im.TimeLeftInSeconds, im.TimeZonesValue, im.TimeZoneoffset, irf.JDTiltle, iss.ThreadId, ca.ResumeLink as resumeLink, 
ulc.FirstName as candidateName, irf.id as irfIntId, irf.AIInterviewType, IRF.IsAIInterview,
case when irf.AIInterviewType = 1 then 1 else 0 end as IsAdaptive
from InterviewMeetings im
join IRF on IRF.irfId = im.irfid
join UserList ulc on ulc.EmailId = im.candidateEmail
join CandidatesMaster cm on cm.UserId = ulc.id
join Candidate ca on (ca.CandidateMasterId = cm.id and ca.IRFId = irf.id)
outer apply (select ThreadId from InterviewSessions where SessionId = @SessionId) iss
where im.SessionId = @SessionId


select * from TransTechSkill where IRFId = @irfId

END
GO


