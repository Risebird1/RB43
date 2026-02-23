
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveAIInterviewProctorEvaluation'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveAIInterviewProctorEvaluation
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveAIInterviewProctorEvaluation]     
@interviewId int,
@InterviewerComment varchar(max),
@MeetingStatus int  = null,
@InterviewStatus int  = null,
@TotalTechScore float = null
As
Begin

-- Validation: Check if AI interview has videos before marking as completed
IF @MeetingStatus = 4
BEGIN
    DECLARE @isAIInterview bit
    DECLARE @sessionId varchar(200)
    DECLARE @videoCount int
    
    SELECT @isAIInterview = ISNULL(irf.IsAIInterview, 0), @sessionId = im.SessionId
    FROM InterviewMeetings im
    JOIN IRF irf on irf.IRFId = im.IRFId
    WHERE im.id = @interviewId
    
    IF @isAIInterview = 1
    BEGIN
        SET @videoCount = (SELECT COUNT(*) FROM BotInterviewQuestionVideos WHERE SessionId = @sessionId AND VideoLink IS NOT NULL AND VideoLink != '')
        
        IF @videoCount = 0
        BEGIN
            RAISERROR ('Cannot mark AI interview as completed: No candidate response videos found in BotInterviewQuestionVideos table for this interview.', 16, 1);
            RETURN;
        END
    END
END

update InterviewComments set MeetingStatus = @MeetingStatus, 
	  InterviewStatus = @InterviewStatus,
	  InterviewerComment = @InterviewerComment,
	  TotalTechScore =@TotalTechScore
	  where InterviewId = @interviewId
if exists(select top 1 id from InterviewReport where InterviewId  = @interviewId)
Begin
 update InterviewReport set ModifiedOn = getdate() where InterviewId = @interviewId
End
Else
Begin
 insert into InterviewReport (InterviewId, IRFId, CreatedOn) values (@interviewId, (select id from IRF where IRFId = (select irfId from InterviewMeetings where id = @interviewId)), getdate())
End
End
GO
