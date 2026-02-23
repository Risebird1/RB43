
IF EXISTS (SELECT 1 FROM sys.triggers 
           WHERE Name = 'tr_InterviewComments_ValidateAIVideoBeforeComplete')
BEGIN
    Drop trigger tr_InterviewComments_ValidateAIVideoBeforeComplete
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER tr_InterviewComments_ValidateAIVideoBeforeComplete
ON InterviewComments
FOR INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @interviewId int
    DECLARE @meetingStatus int
    DECLARE @isAIInterview bit
    DECLARE @sessionId varchar(200)
    DECLARE @videoCount int
    
    SELECT @interviewId = InterviewId, @meetingStatus = MeetingStatus
    FROM inserted
    
    IF @meetingStatus = 4
    BEGIN
        SELECT @isAIInterview = ISNULL(irf.IsAIInterview, 0), @sessionId = im.SessionId
        FROM InterviewMeetings im
        JOIN IRF irf on irf.IRFId = im.IRFId
        WHERE im.id = @interviewId
        
        IF @isAIInterview = 1
        BEGIN
            SET @videoCount = (SELECT COUNT(*) 
                              FROM BotInterviewQuestionVideos 
                              WHERE SessionId = @sessionId 
                              AND VideoLink IS NOT NULL 
                              AND VideoLink != '')
            
            IF @videoCount = 0
            BEGIN
                RAISERROR ('Cannot mark AI interview as completed: No candidate response videos found in BotInterviewQuestionVideos table for this interview. Please ensure the candidate has recorded at least one video response before marking the interview as complete.', 16, 1);
                ROLLBACK TRANSACTION;
                RETURN;
            END
        END
    END
END
GO

