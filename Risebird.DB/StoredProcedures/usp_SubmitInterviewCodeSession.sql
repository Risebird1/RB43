 IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SubmitInterviewCodeSession'))
BEGIN
    DROP PROCEDURE dbo.usp_SubmitInterviewCodeSession
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- The candidate's submitted answer for a code session. A later submit (before the token expires)
-- replaces the earlier one; the report shows the latest submission next to the last run's output.
CREATE PROCEDURE [dbo].[usp_SubmitInterviewCodeSession]
    @Token       UNIQUEIDENTIFIER,
    @LanguageId  VARCHAR(40),
    @Source      NVARCHAR(MAX),
    @CurrentDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    -- A submission is final: once SubmittedOn is set the candidate's code can no longer be replaced - not by the
    -- candidate, and not by anyone else who opens the same link (an interviewer included). Only the first submit saves.
    UPDATE dbo.InterviewCodeSessions
       SET LanguageId = @LanguageId,
           SubmittedSource = @Source,
           SubmittedOn = @CurrentDate,
           OpenedOn = ISNULL(OpenedOn, @CurrentDate)
     WHERE Token = @Token
       AND SubmittedOn IS NULL;

    DECLARE @Accepted BIT = CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END;

    -- Accepted = 0 with a SubmittedOn means it had already been submitted; the stored code was left as it was.
    SELECT SubmittedOn, @Accepted AS Accepted FROM dbo.InterviewCodeSessions WHERE Token = @Token;
END
GO
