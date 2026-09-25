IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetReportOfAIInterviewByInterviewId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetReportOfAIInterviewByInterviewId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- The stored AI evaluation for an interview, read by interview id.
--
-- usp_GetReportOfAIInterviewByAssistant does the same thing keyed by SessionId, which the report
-- screens do not carry: ClientReportV3 is opened with an interview id and an IRF id. Rather than
-- resolve a session from the interview and back again, this reads the same column directly.
--
-- Read-only and additive; nothing existing changes.
Create Procedure usp_GetReportOfAIInterviewByInterviewId
@InterviewId int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 ReportOfAIInterviewByAssistant
    FROM InterviewComments
    WHERE InterviewId = @InterviewId
      AND ReportOfAIInterviewByAssistant IS NOT NULL
    ORDER BY id DESC;
END
GO
