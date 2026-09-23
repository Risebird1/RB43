IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CreateInterviewCodeSession'))
BEGIN
    DROP PROCEDURE [dbo].[usp_CreateInterviewCodeSession]
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================================================
-- Creates one code session (one attempt) against one question.
--
-- Generating a session never replaces or invalidates an earlier one: a candidate may
-- fumble an attempt, a link may be lost, an interviewer may want a second run at the same
-- problem. AttemptNumber is MAX+1 within (InterviewId, question), so the report can label
-- a progression as a progression.
--
-- QuestionText is stored as a SNAPSHOT. The bank entry can be edited afterwards; the
-- report must show what was actually put to this candidate.
--
-- ExpiresAt is the interview's end plus a grace window. The link is unauthenticated, so it
-- must stop working when the interview does.
--
-- Gated: the caller must be the assigned interviewer AND the channel switch must be on.
-- Returns the created row, or no rows when the gate fails.
-- =============================================================================
CREATE PROCEDURE [dbo].[usp_CreateInterviewCodeSession]
    @InterviewId       int,
    @InterviewerEmail  varchar(200),
    @SampleQuestionId  int           = null,
    @QuestionText      nvarchar(max) = null,
    @SkillId           int           = null,
    @SkillName         nvarchar(200) = null,
    @Language          varchar(50)   = null,
    @TimeLimitMinutes  int           = null,
    @SessionToken      varchar(64),
    @TimeNow           datetime      = null,
    @GraceMinutes      int           = 180
AS
BEGIN
    SET NOCOUNT ON;

    IF @TimeNow IS NULL SET @TimeNow = GETDATE();

    DECLARE @IrfIntId int, @ExpiresAt datetime, @AttemptNumber int;

    -- Gate: assigned interviewer, and code sessions enabled on the channel. Off at channel
    -- level means no sessions anywhere on that channel, whatever the per-question flags say.
    SELECT TOP 1
           @IrfIntId  = irf.id,
           @ExpiresAt = DATEADD(minute,
                                ISNULL(NULLIF(irf.InterviewDuration, 0), ISNULL(im.interviewDuration, 60)) + @GraceMinutes,
                                im.interviewDate)
      FROM InterviewMeetings im
      JOIN IRF irf ON irf.IRFId = im.IRFId
     WHERE im.id = @InterviewId
       AND im.interviewer = @InterviewerEmail
       AND ISNULL(irf.CodeSessionsEnabled, 0) = 1;

    IF @IrfIntId IS NULL
        RETURN;

    -- Attempt order is scoped to this interview and THIS QUESTION.
    --
    -- A bank question is identified by its id. An ad-hoc question has none - every ad-hoc
    -- session carries a NULL SampleQuestionId - so matching on the id alone would put every
    -- improvised question in one shared sequence: ask a second, unrelated ad-hoc question and
    -- its first attempt is labelled "Attempt 2", which reads in the client report as a retry
    -- of a question the candidate was never asked. The question text is the only identity an
    -- ad-hoc question has, so that is what scopes it.
    IF @SampleQuestionId IS NULL
    BEGIN
        SELECT @AttemptNumber = ISNULL(MAX(AttemptNumber), 0) + 1
          FROM InterviewCodeSession
         WHERE InterviewId = @InterviewId
           AND SampleQuestionId IS NULL
           AND ISNULL(QuestionText, N'') = ISNULL(@QuestionText, N'');
    END
    ELSE
    BEGIN
        SELECT @AttemptNumber = ISNULL(MAX(AttemptNumber), 0) + 1
          FROM InterviewCodeSession
         WHERE InterviewId = @InterviewId
           AND SampleQuestionId = @SampleQuestionId;
    END

    INSERT INTO InterviewCodeSession
        (InterviewId, IRFId, SampleQuestionId, SkillId, SkillName, QuestionText,
         SessionToken, AttemptNumber, SessionState, Language, TimeLimitMinutes,
         PasteEvents, CreatedBy, CreatedAt, ExpiresAt)
    VALUES
        (@InterviewId, @IrfIntId, @SampleQuestionId, @SkillId, @SkillName, @QuestionText,
         @SessionToken, @AttemptNumber, 1, @Language, @TimeLimitMinutes,
         0, @InterviewerEmail, @TimeNow, @ExpiresAt);

    SELECT TOP 1 *
      FROM InterviewCodeSession
     WHERE SessionToken = @SessionToken;
END
GO
