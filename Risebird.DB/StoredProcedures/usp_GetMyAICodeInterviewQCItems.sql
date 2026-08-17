IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetMyAICodeInterviewQCItems'))
BEGIN
    DROP PROCEDURE dbo.usp_GetMyAICodeInterviewQCItems
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- AI code interview audits assigned to the signed in expert. WasDiscrepancyFound and MaxSeverity
-- come straight off the assignment row so the listing can badge severe disagreements without
-- reading the discrepancy rows.
-- @isCompleted: 0 = not yet submitted, 1 = submitted, NULL = both.
Create Procedure usp_GetMyAICodeInterviewQCItems
@emailID varchar(150),
@isCompleted bit = NULL
AS
BEGIN
    SET NOCOUNT ON;

    select
        im.SessionId,
        im.id as InterviewId,
        irf.id as irfIntId,
        irf.IRFId,
        irf.JDTiltle,
        im.candidateName,
        im.candidateEmail,
        im.interviewDate,
        a.id as AssignmentId,
        a.ExpertEmail,
        a.AssignedAt,
        a.SubmittedAt,
        a.WasDiscrepancyFound,
        a.MaxSeverity,
        (select count(1) from BotInterviewQuestionVideos bq where bq.SessionId = im.SessionId) as TotalQuestions
    from AICodeInterviewExpertAssignment a
    join InterviewMeetings im on im.SessionId = a.SessionId
    join IRF irf on irf.IRFId = im.IRFId
    where a.IsActive = 1
      and a.ExpertEmail = @emailID
      and (@isCompleted is null
           or (@isCompleted = 1 and a.SubmittedAt is not null)
           or (@isCompleted = 0 and a.SubmittedAt is null))
    order by a.AssignedAt desc
END
GO
