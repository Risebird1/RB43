IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetReportOfAIInterviewByAssistant'))
BEGIN
    DROP PROCEDURE dbo.usp_GetReportOfAIInterviewByAssistant
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetReportOfAIInterviewByAssistant
@SessionId varchar(200)
AS
BEGIN

declare @interviewId int

set @interviewId = (select id from InterviewMeetings where SessionId = @SessionId)

select ReportOfAIInterviewByAssistant from InterviewComments where InterviewId = @interviewId

END
GO

