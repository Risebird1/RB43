IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ValidateInterviewForCancel'))
BEGIN
    DROP PROCEDURE dbo.usp_ValidateInterviewForCancel
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_ValidateInterviewForCancel]
    @interviewid INT,
    @CandidateEmail VARCHAR(200)
AS
BEGIN
    SELECT id
    FROM InterviewMeetings
    WHERE id = @interviewid
      AND CandidateEmail = @CandidateEmail
      AND confirmationStatus = 1
END
GO
