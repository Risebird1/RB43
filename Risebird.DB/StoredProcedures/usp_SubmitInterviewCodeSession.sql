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

    UPDATE dbo.InterviewCodeSessions
       SET LanguageId = @LanguageId,
           SubmittedSource = @Source,
           SubmittedOn = @CurrentDate,
           OpenedOn = ISNULL(OpenedOn, @CurrentDate)
     WHERE Token = @Token;

    SELECT SubmittedOn FROM dbo.InterviewCodeSessions WHERE Token = @Token;
END
GO
