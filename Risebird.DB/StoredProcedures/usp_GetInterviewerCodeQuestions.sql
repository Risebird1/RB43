IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewerCodeQuestions'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerCodeQuestions
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- "Your questions" on the interview alignment page: the signed-in interviewer's own TechSkillComments
-- rows for the interview's channel - the same rows IRF/SampleQuestions lets them edit - with the code
-- session already minted for this interview, if any. Unlike usp_GetTechSkillQuestionsByUserId there is
-- no admin widening: this list is always the interviewer's own, because only their own rows can be
-- minted. Client samples (SkillChannelSampleQuestions) are deliberately not part of it.
CREATE PROCEDURE [dbo].[usp_GetInterviewerCodeQuestions]
    @InterviewId      INT,
    @InterviewerEmail VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Email VARCHAR(200) = LTRIM(RTRIM(REPLACE(ISNULL(@InterviewerEmail, ''), '"', '')));
    DECLARE @IrfIntId INT;
    DECLARE @UserId INT;

    SELECT @IrfIntId = irf.id
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    WHERE im.id = @InterviewId;

    SELECT TOP 1 @UserId = ul.id
    FROM dbo.UserList ul
    WHERE LTRIM(RTRIM(ul.EmailId)) = @Email
    ORDER BY ul.id;

    SELECT tsc.id AS QuestionId,
           tsc.Comment,
           tsc.SkillName,
           CAST(ISNULL(tsc.IsCodingQuestion, 0) AS BIT) AS IsCodingQuestion,
           tsc.CodingTimeLimitInMinutes,
           ics.Token,
           ics.RunCount,
           ics.LastRunStatus,
           ics.OpenedOn,
           ics.SubmittedOn
    FROM dbo.TechSkillComments tsc
    LEFT JOIN dbo.InterviewCodeSessions ics
        ON ics.InterviewId = @InterviewId AND ics.QuestionId = tsc.id
    WHERE tsc.IRFId = @IrfIntId
      AND tsc.UserId = @UserId
      AND @UserId IS NOT NULL
    ORDER BY tsc.id ASC;
END
GO
