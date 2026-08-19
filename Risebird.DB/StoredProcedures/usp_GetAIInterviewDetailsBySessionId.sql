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
  
declare @irfId int, @aiInterviewType int, @biqvId int   
set @irfId  = (select id from IRF where IRFId = (select IRFId from InterviewMeetings where SessionId =@SessionId))  

set @biqvId  = (select top 1 biqvId from VideoSentimentAnalysis where biqvId  in (select id from BotInterviewQuestionVideos where SessionId =@SessionId))  
  
set @aiInterviewType  = (select AIInterviewType from IRF where id = @irfId)

  
select im.id as InterviewId, im.interviewDate, im.AssistantId, im.candidateEmail, ulc.MobileNumber, im.interviewer,  
im.TimeLeftInSeconds, im.TimeZonesValue, im.TimeZoneoffset, irf.JDTiltle, irf.JDLink, iss.ThreadId, ca.ResumeLink as resumeLink,
ISNULL(ad.matchPercentage, 0) as MatchScorePercentage,
ISNULL(ad.id, 0) as AadharVerificationId, ic.MeetingStatus, ic.InterviewStatus,
case when (im.candidateName is not null and im.candidateName != '')  then im.candidateName else ulc.FirstName end as candidateName,
irf.id as irfIntId, irf.AIInterviewType, IRF.IsAIInterview,  irf.OpenAIAssistantId, irf.OpenAIPromptId, irf.showChatBox, irf.isMultiLingual,
case when irf.AIInterviewType = 1 then 1 else 0 end as IsAdaptive, @biqvId as CandidateAnswerVideoIdAI,
ca.AIResumeScreeningVerdict, ca.AIResumeScreeningScore
from InterviewMeetings im
left join InterviewComments ic on ic.InterviewId = im.id
left join AadhaarDetails ad on (ad.email = im.candidateEmail and ad.reference_id = CAST(im.id AS varchar(50)))
join IRF on IRF.irfId = im.irfid  
join UserList ulc on ulc.EmailId = im.candidateEmail  
join CandidatesMaster cm on cm.UserId = ulc.id  
join Candidate ca on (ca.CandidateMasterId = cm.id and ca.IRFId = irf.id)  
outer apply (select ThreadId from InterviewSessions where SessionId = @SessionId) iss  
where im.SessionId = @SessionId  
  
  
select * from TransTechSkill where IRFId = @irfId  
  
if(@aiInterviewType = 2) -- 2 means preset  
Begin  

select bq.question, bq.CandidateAnswer, sq.ExpectedAnswer, sq.SkillName from BotInterviewQuestionVideos bq   
join SkillChannelSampleQuestions sq on bq.DeliveryHeadQuestionId = sq.id  
where irfid = @irfId  and SessionId = @SessionId
  
End  
  
END
GO


