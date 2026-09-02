IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveAIInterviewMcqAssessment'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveAIInterviewMcqAssessment
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Caches the MCQ section result fetched from screen.risebird.io for one interview session.
-- Last write wins: the newest successful fetch is always the one worth keeping.
CREATE PROCEDURE [dbo].[usp_SaveAIInterviewMcqAssessment]
    @SessionId       varchar(50),
    @InterviewId     int = NULL,
    @IRFId           int = NULL,
    @McqId           varchar(50) = NULL,
    @Status          varchar(20) = NULL,
    @TotalQuestions  int = NULL,
    @CorrectAnswers  int = NULL,
    @ScorePercent    decimal(5,2) = NULL,
    @RedFlags        int = NULL,
    @AllottedMinutes int = NULL,
    @ElapsedSeconds  int = NULL,
    @TabSwitchCount  int = NULL,
    @StartedOn       datetime = NULL,
    @CompletedOn     datetime = NULL,
    @ReportJson      nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE AIInterviewMcqAssessment
       SET InterviewId     = ISNULL(@InterviewId, InterviewId),
           IRFId           = ISNULL(@IRFId, IRFId),
           McqId           = ISNULL(@McqId, McqId),
           Status          = @Status,
           TotalQuestions  = @TotalQuestions,
           CorrectAnswers  = @CorrectAnswers,
           ScorePercent    = @ScorePercent,
           RedFlags        = @RedFlags,
           AllottedMinutes = @AllottedMinutes,
           ElapsedSeconds  = @ElapsedSeconds,
           TabSwitchCount  = @TabSwitchCount,
           StartedOn       = @StartedOn,
           CompletedOn     = @CompletedOn,
           ReportJson      = @ReportJson,
           FetchedOn       = GETDATE()
     WHERE SessionId = @SessionId;

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO AIInterviewMcqAssessment
            (SessionId, InterviewId, IRFId, McqId, Status, TotalQuestions, CorrectAnswers,
             ScorePercent, RedFlags, AllottedMinutes, ElapsedSeconds, TabSwitchCount,
             StartedOn, CompletedOn, ReportJson, FetchedOn)
        VALUES
            (@SessionId, @InterviewId, @IRFId, @McqId, @Status, @TotalQuestions, @CorrectAnswers,
             @ScorePercent, @RedFlags, @AllottedMinutes, @ElapsedSeconds, @TabSwitchCount,
             @StartedOn, @CompletedOn, @ReportJson, GETDATE());
    END
END
GO
