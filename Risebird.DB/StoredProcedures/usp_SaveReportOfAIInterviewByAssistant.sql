IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveReportOfAIInterviewByAssistant'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveReportOfAIInterviewByAssistant
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_SaveReportOfAIInterviewByAssistant
@SessionId varchar(200),
@SerializedReportOfAIInterviewByAssistant varchar(max)
AS
BEGIN
declare @interviewId int

set @interviewId = (select id from InterviewMeetings where SessionId = @SessionId)

update InterviewComments set ReportOfAIInterviewByAssistant = @SerializedReportOfAIInterviewByAssistant where InterviewId  = @interviewId

END
GO

