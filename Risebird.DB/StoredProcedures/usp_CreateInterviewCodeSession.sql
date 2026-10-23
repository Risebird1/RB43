IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_CreateInterviewCodeSession'))
BEGIN
    DROP PROCEDURE dbo.usp_CreateInterviewCodeSession
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Mints a code session for one of the interviewer's own questions, from the interview alignment page.
-- One request does all of it, in one transaction:
--   1. @QuestionId = 0: the typed question is written to TechSkillComments for this interviewer and
--      channel first, so it is in their list on the next interview for the channel.
--      @QuestionId > 0: the row must belong to this interviewer on this channel - never another
--      interviewer's question and never a client sample (SkillChannelSampleQuestions is not read).
--   2. the row is marked IsCodingQuestion = 1 with its time limit (no question-gist requirement).
--   3. a session is created for (interview, question), or the existing one is returned.
--
-- The interviewer and channel are resolved from the interview and the signed-in email, never taken
-- from the request. Status: 'ok', 'not_interviewer', 'not_owner', 'empty_question'.
CREATE PROCEDURE [dbo].[usp_CreateInterviewCodeSession]
    @InterviewId      INT,
    @InterviewerEmail VARCHAR(200),
    @QuestionId       INT,
    @QuestionText     NVARCHAR(MAX),
    @SkillName        VARCHAR(500),
    @TimeLimit        INT,
    @Token            UNIQUEIDENTIFIER,
    @CurrentDate      DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Email VARCHAR(200) = LTRIM(RTRIM(REPLACE(ISNULL(@InterviewerEmail, ''), '"', '')));
    DECLARE @IrfIntId INT;
    DECLARE @UserId INT;

    SELECT @IrfIntId = irf.id
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    WHERE im.id = @InterviewId
      AND LTRIM(RTRIM(REPLACE(ISNULL(im.interviewer, ''), '"', ''))) = @Email;

    SELECT TOP 1 @UserId = ul.id
    FROM dbo.UserList ul
    WHERE LTRIM(RTRIM(ul.EmailId)) = @Email
    ORDER BY ul.id;

    IF @IrfIntId IS NULL OR @UserId IS NULL OR @Email = ''
    BEGIN
        SELECT CAST('not_interviewer' AS VARCHAR(20)) AS Status;
        RETURN;
    END

    BEGIN TRAN;

    IF ISNULL(@QuestionId, 0) = 0
    BEGIN
        IF LTRIM(RTRIM(ISNULL(@QuestionText, N''))) = N''
        BEGIN
            ROLLBACK TRAN;
            SELECT CAST('empty_question' AS VARCHAR(20)) AS Status;
            RETURN;
        END

        INSERT INTO dbo.TechSkillComments (IRFId, Skill, Comment, UserId, SkillName, IsCodingQuestion, CodingTimeLimitInMinutes)
        VALUES (@IrfIntId, NULL, LTRIM(RTRIM(@QuestionText)), @UserId, @SkillName, 1, @TimeLimit);

        SET @QuestionId = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.TechSkillComments tsc
                       WHERE tsc.id = @QuestionId AND tsc.IRFId = @IrfIntId AND tsc.UserId = @UserId)
        BEGIN
            ROLLBACK TRAN;
            SELECT CAST('not_owner' AS VARCHAR(20)) AS Status;
            RETURN;
        END

        UPDATE dbo.TechSkillComments
           SET IsCodingQuestion = 1,
               CodingTimeLimitInMinutes = ISNULL(@TimeLimit, CodingTimeLimitInMinutes)
         WHERE id = @QuestionId;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.InterviewCodeSessions WITH (UPDLOCK, HOLDLOCK)
                   WHERE InterviewId = @InterviewId AND QuestionId = @QuestionId)
    BEGIN
        -- The snapshot keeps the text exactly as typed when the question was just written (TechSkillComments.Comment
        -- is varchar and would lose non-Latin characters); an existing row is snapshotted as it stands.
        INSERT INTO dbo.InterviewCodeSessions (InterviewId, QuestionId, QuestionText, Token, CodingTimeLimitInMinutes, CreatedBy, CreatedOn)
        SELECT @InterviewId, tsc.id,
               COALESCE(NULLIF(LTRIM(RTRIM(@QuestionText)), N''), CAST(tsc.Comment AS NVARCHAR(MAX)), N''),
               @Token, tsc.CodingTimeLimitInMinutes, @Email, @CurrentDate
        FROM dbo.TechSkillComments tsc
        WHERE tsc.id = @QuestionId;
    END

    COMMIT TRAN;

    SELECT CAST('ok' AS VARCHAR(20)) AS Status,
           ics.Id, ics.InterviewId, ics.QuestionId, ics.QuestionText, ics.Token, ics.CodingTimeLimitInMinutes,
           ics.LanguageId, ics.RunCount, ics.LastRunStatus, ics.CreatedOn, ics.OpenedOn, ics.SubmittedOn
    FROM dbo.InterviewCodeSessions ics
    WHERE ics.InterviewId = @InterviewId AND ics.QuestionId = @QuestionId;
END
GO
