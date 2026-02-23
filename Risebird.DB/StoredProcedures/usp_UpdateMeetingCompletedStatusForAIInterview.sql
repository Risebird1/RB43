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
@timeNow datetime = null,
@ModifiedBy varchar(100) = 'System'
AS
BEGIN
declare @interviewId int
declare @videoCount int
declare @isAIInterview bit

set @interviewId = (select id from InterviewMeetings where SessionId = @SessionId)

select @isAIInterview = ISNULL(irf.IsAIInterview, 0)
from InterviewMeetings im
join IRF irf on irf.IRFId = im.IRFId
where im.id = @interviewId

if @isAIInterview = 1
begin
    set @videoCount = (select count(*) from BotInterviewQuestionVideos where SessionId = @SessionId and VideoLink is not null and VideoLink != '')
    
    if @videoCount = 0
    begin
        RAISERROR ('Cannot mark AI interview as completed: No candidate response videos found in BotInterviewQuestionVideos table for this interview.', 16, 1);
        return;
    end
end

update InterviewComments set MeetingStatus = 4, ModifiedOn = @timeNow, ModifiedBy = @ModifiedBy  where InterviewId  = @interviewId

END
GO

