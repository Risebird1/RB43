IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateMeetingCompletedStatusForAIInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateMeetingCompletedStatusForAIInterview
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_UpdateMeetingCompletedStatusForAIInterview
@SessionId varchar(200),
@timeNow datetime = null
AS
BEGIN
declare @interviewId int

set @interviewId = (select id from InterviewMeetings where SessionId = @SessionId)

update InterviewComments set MeetingStatus = 4, ModifiedOn = @timeNow  where InterviewId  = @interviewId

END
GO

