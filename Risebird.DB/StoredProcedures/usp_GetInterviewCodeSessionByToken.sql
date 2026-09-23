IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewCodeSessionByToken'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetInterviewCodeSessionByToken]
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================================================
-- Resolves the candidate's unauthenticated link.
--
-- The token IS the security boundary, so this returns exactly one session and nothing
-- else: no candidate identity, no channel, no sibling sessions in the same interview. The
-- SELECT list is deliberately explicit rather than SELECT * for that reason.
--
-- Marks the session Opened on first resolve so the interviewer can see it was picked up.
-- A session past ExpiresAt is reported as Expired (5) without being rewritten, so the
-- stored record of what happened during the interview stays untouched.
-- =============================================================================
CREATE PROCEDURE [dbo].[usp_GetInterviewCodeSessionByToken]
    @SessionToken varchar(64),
    @TimeNow      datetime = null
AS
BEGIN
    SET NOCOUNT ON;
    IF @TimeNow IS NULL SET @TimeNow = GETDATE();

    -- First open moves Generated -> Opened. Never moves a submitted session back.
    UPDATE InterviewCodeSession
       SET SessionState = 2,
           OpenedAt     = @TimeNow
     WHERE SessionToken = @SessionToken
       AND SessionState = 1
       AND (ExpiresAt IS NULL OR ExpiresAt > @TimeNow);

    SELECT TOP 1
        SessionToken,
        QuestionText,
        SkillName,
        Language,
        SubmittedCode,
        Stdout,
        Stderr,
        RunState,
        TimeLimitMinutes,
        -- The countdown runs from when the session was first opened, not from when this page
        -- happened to load, so reloading the tab cannot hand the candidate a fresh allowance.
        OpenedAt,
        -- Seeds the editor's paste counter. Without it a reload restarts counting at zero and
        -- the next few pastes are swallowed by the save procedure's "only ever grows" guard.
        PasteEvents,
        ExpiresAt,
        CASE WHEN ExpiresAt IS NOT NULL AND ExpiresAt <= @TimeNow AND SessionState <> 4
             THEN 5 ELSE SessionState END AS SessionState,
        CAST(1 AS bit) AS IsValid
      FROM InterviewCodeSession
     WHERE SessionToken = @SessionToken;
END
GO
