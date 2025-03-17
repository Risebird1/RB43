IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateCandidateAcknowledge'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateCandidateAcknowledge
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_UpdateCandidateAcknowledge
@interviewid int,
@interviewchecks varchar(1000)
AS
BEGIN
Update InterviewMeetings set CandidateInterviewChecks=@interviewchecks where id=@interviewchecks
END
Go