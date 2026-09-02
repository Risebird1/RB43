IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAIInterviewMcqAssessment'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAIInterviewMcqAssessment
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- The cached MCQ section result for one interview session, used when the live fetch from
-- screen.risebird.io fails or when rendering a historical report.
CREATE PROCEDURE [dbo].[usp_GetAIInterviewMcqAssessment]
    @SessionId varchar(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
           SessionId, InterviewId, IRFId, McqId, Status, TotalQuestions, CorrectAnswers,
           ScorePercent, RedFlags, AllottedMinutes, ElapsedSeconds, TabSwitchCount,
           StartedOn, CompletedOn, ReportJson, FetchedOn
    FROM AIInterviewMcqAssessment
    WHERE SessionId = @SessionId;
END
GO
