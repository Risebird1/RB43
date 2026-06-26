IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetFutureAssignedInterviewsForInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_GetFutureAssignedInterviewsForInterview
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetFutureAssignedInterviewsForInterview]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IRFName VARCHAR(50);
    DECLARE @Interviewer VARCHAR(200);
    DECLARE @timeNow DATETIME = GETDATE();

    SELECT @IRFName = IRFID, @Interviewer = interviewer
    FROM dbo.InterviewMeetings
    WHERE id = @InterviewId;

    SELECT COUNT(1) AS FutureAssignedCount
    FROM dbo.InterviewMeetings
    WHERE interviewdate > @timeNow
      AND IRFId = @IRFName
      AND Interviewer = @Interviewer;
END
GO
