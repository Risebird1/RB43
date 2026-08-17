IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetOpenAICodeInterviewQCItems'))
BEGIN
    DROP PROCEDURE dbo.usp_GetOpenAICodeInterviewQCItems
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Completed AI code interviews that are waiting for an expert to pick them up, scoped to the
-- skill channels the signed in user is an SME for. Mirrors usp_OpenQCInterviews, which is the
-- equivalent query for interviewer led QC.
--
-- An interview qualifies as an "AI code interview" when its skill channel is flagged for AI
-- interviewing in Preset mode and has at least one coding sample question.
Create Procedure usp_GetOpenAICodeInterviewQCItems
@emailID varchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    select distinct
        im.SessionId,
        im.id as InterviewId,
        irf.id as irfIntId,
        irf.IRFId,
        irf.JDTiltle,
        im.candidateName,
        im.candidateEmail,
        im.interviewDate,
        (select count(1) from BotInterviewQuestionVideos bq where bq.SessionId = im.SessionId) as TotalQuestions
    from InterviewMeetings im
    join IRF irf on irf.IRFId = im.IRFId
    join InterviewComments ic on ic.InterviewId = im.id
    join SkillChannelSMEs smm on smm.IRFId = irf.id
    left join InterviewerProfile ip on ip.id = smm.InterviewerProfileId
    join UserList ul on ul.id = ip.UserId
    where im.SessionId is not null and im.SessionId <> ''
      and isnull(irf.IsAIInterview, 0) = 1
      and irf.AIInterviewType = 2 -- 2 means preset
      and exists (
            select 1 from SkillChannelSampleQuestions sq
            where sq.IRFId = irf.id and isnull(sq.IsCodingQuestion, 0) = 1)
      and ic.MeetingStatus = 4 -- 4 means completed
      and ic.ReportOfAIInterviewByAssistant is not null
      and ic.ReportOfAIInterviewByAssistant <> ''
      and ul.IsActive = 1
      and ul.EmailId = @emailID
      and not exists (
            select 1 from AICodeInterviewExpertAssignment a
            where a.SessionId = im.SessionId and a.IsActive = 1)
    order by im.interviewDate desc
END
GO
