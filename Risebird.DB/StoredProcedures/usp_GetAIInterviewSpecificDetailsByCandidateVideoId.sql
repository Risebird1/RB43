IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAIInterviewSpecificDetailsByCandidateVideoId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAIInterviewSpecificDetailsByCandidateVideoId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetAIInterviewSpecificDetailsByCandidateVideoId
@biqvId int = null
AS
BEGIN

declare @SessionId varchar(200) 

set @SessionId = (select SessionId from BotInterviewQuestionVideos where id = @biqvId)

select im.id as InterviewId, im.interviewDate, im.AssistantId, im.candidateEmail, ulc.MobileNumber, im.interviewer,  
im.TimeLeftInSeconds, im.TimeZonesValue, im.TimeZoneoffset, irf.JDTiltle, ca.ResumeLink as resumeLink, im.IRFId,
(select VideoLink from BotInterviewQuestionVideos where id = @biqvId) as CandidateAnswerVideoLink, @biqvId as CandidateAnswerVideoIdAI,
--ISNULL(ad.matchPercentage, 0) as MatchScorePercentage,
--ISNULL(ad.id, 0) as AadharVerificationId,
ic.MeetingStatus, ic.InterviewStatus,
case when (im.candidateName is not null and im.candidateName != '')  then im.candidateName else ulc.FirstName end as candidateName,
irf.id as irfIntId, irf.AIInterviewType, IRF.IsAIInterview,  
case when irf.AIInterviewType = 1 then 1 else 0 end as IsAdaptive  
from InterviewMeetings im
left join InterviewComments ic on ic.InterviewId = im.id
--left join AadhaarDetails ad on (ad.email = im.candidateEmail and ad.reference_id = CAST(im.id AS varchar(50)))
join IRF on IRF.irfId = im.irfid  
join UserList ulc on ulc.EmailId = im.candidateEmail  
join CandidatesMaster cm on cm.UserId = ulc.id  
join Candidate ca on (ca.CandidateMasterId = cm.id and ca.IRFId = irf.id)   
where im.SessionId = @SessionId

END
GO