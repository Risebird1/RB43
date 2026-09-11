IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewCodeSessions'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetInterviewCodeSessions]
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================================================
-- All code sessions for one interview, oldest first.
--
-- Serves two readers from one round trip:
--   the alignment page, which lists every session per question with its state and link;
--   the client report, which filters to submitted ones only.
--
-- The submitted-only filter is applied by the caller, not here, so the interviewer can see
-- generated-but-never-opened sessions that a client never will. Keeping every session is a
-- storage decision; showing one to a client is a different decision.
-- =============================================================================
CREATE PROCEDURE [dbo].[usp_GetInterviewCodeSessions]
    @InterviewId int,
    @TimeNow     datetime = null
AS
BEGIN
    SET NOCOUNT ON;
    IF @TimeNow IS NULL SET @TimeNow = GETDATE();

    SELECT
        id, InterviewId, IRFId, SampleQuestionId, SkillId, SkillName, QuestionText,
        SessionToken, AttemptNumber,
        -- Report an unsubmitted session past its expiry as Expired without rewriting the row.
        CASE WHEN ExpiresAt IS NOT NULL AND ExpiresAt <= @TimeNow AND SessionState <> 4
             THEN 5 ELSE SessionState END AS SessionState,
        Language, LanguageVersionUsed, SubmittedCode, Stdout, Stderr,
        ExitStatus, RunState, RunDurationMs, PasteEvents, TimeLimitMinutes,
        CreatedBy, CreatedAt, OpenedAt, LastSavedAt, SubmittedAt, ExpiresAt
      FROM InterviewCodeSession
     WHERE InterviewId = @InterviewId
     ORDER BY ISNULL(SkillName, ''), ISNULL(SampleQuestionId, 0), AttemptNumber;
END
GO
