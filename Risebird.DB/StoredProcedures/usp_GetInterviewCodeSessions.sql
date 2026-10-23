IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewCodeSessions'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewCodeSessions
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Every code session minted for an interview, oldest first. Read by the interview alignment page (the
-- interviewer's own interview, ownership checked by the caller) and by the client report views, which
-- render it under their own access control. QuestionText is the snapshot taken when the session was
-- minted, so the report shows what the candidate was actually asked.
CREATE PROCEDURE [dbo].[usp_GetInterviewCodeSessions]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ics.Id, ics.InterviewId, ics.QuestionId, ics.QuestionText, ics.Token, ics.LanguageId,
           ics.SubmittedSource, ics.LastRunSource, ics.LastRunOutput, ics.LastRunStatus, ics.RunCount,
           ics.CodingTimeLimitInMinutes, ics.CreatedBy, ics.CreatedOn, ics.OpenedOn, ics.SubmittedOn
    FROM dbo.InterviewCodeSessions ics
    WHERE ics.InterviewId = @InterviewId
    ORDER BY ics.CreatedOn ASC, ics.Id ASC;
END
GO
