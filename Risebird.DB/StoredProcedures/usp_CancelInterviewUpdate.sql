IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CancelInterviewUpdate'))
BEGIN
    DROP PROCEDURE dbo.usp_CancelInterviewUpdate
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_CancelInterviewUpdate
@interviewid int,
@CandidateEmail varchar(200)
AS
BEGIN
Update InterviewMeetings set confirmationStatus=2 where id=@interviewid and CandidateEmail=@CandidateEmail
END
