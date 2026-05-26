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
@SerializedReportOfAIInterviewByAssistant nvarchar(max) = null,
@isExit bit = null,
@interviewStatus int = null,
@interviewerComment varchar(1000) = null,
@meetingStatus int = null,
@timeNow datetime = null
AS
BEGIN
declare @interviewId int

set @interviewId = (select id from InterviewMeetings where SessionId = @SessionId)

if @interviewId is null
Begin
	return
End

if not exists (select top 1 id from BotInterviewQuestionVideos where SessionId = @SessionId)
Begin
	set @interviewStatus = null
End

-- Ensure InterviewComments row exists before attempting update
IF NOT EXISTS (SELECT 1 FROM InterviewComments WHERE InterviewId = @interviewId)
BEGIN
	INSERT INTO InterviewComments (InterviewId, MeetingStatus, CreatedOn)
	VALUES (@interviewId, 2, ISNULL(@timeNow, GETDATE()))
END

if(@meetingStatus is null)
Begin
	update InterviewComments set ReportOfAIInterviewByAssistant = @SerializedReportOfAIInterviewByAssistant,
	HasCandidateExited = @isExit, CandidateExitedAt = @timeNow,
	MeetingStatus = CASE 
							WHEN (@interviewStatus IS NOT NULL and (@interviewStatus = 2 or @interviewStatus = 3)) THEN 4 
							ELSE MeetingStatus 
					END,
	InterviewStatus = @interviewStatus, InterviewerComment = @interviewerComment,
	ModifiedOn = @timeNow  where InterviewId  = @interviewId
End
else
begin
	update InterviewComments set ReportOfAIInterviewByAssistant = @SerializedReportOfAIInterviewByAssistant,
	HasCandidateExited = @isExit, CandidateExitedAt = @timeNow,
	MeetingStatus = @meetingStatus,
	InterviewStatus = @interviewStatus, InterviewerComment = @interviewerComment,
	ModifiedOn = @timeNow  where InterviewId  = @interviewId
end

-- Ensure InterviewReport row exists
IF NOT EXISTS (SELECT 1 FROM InterviewReport WHERE InterviewId = @interviewId)
BEGIN
	declare @irfIntId int
	set @irfIntId = (select id from IRF where IRFId = (select IRFId from InterviewMeetings where id = @interviewId))
	IF @irfIntId IS NOT NULL
	BEGIN
		INSERT INTO InterviewReport (InterviewId, IRFId, CreatedOn)
		VALUES (@interviewId, @irfIntId, ISNULL(@timeNow, GETDATE()))
	END
END

END
GO

