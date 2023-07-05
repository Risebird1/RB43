IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateReSchedulingInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateReSchedulingInterview
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_UpdateReSchedulingInterview
@interviewid int,
@source varchar(100)
AS
BEGIN
Update InterviewMeetings set ConfirmationStatus=2,Cancellationsource=@source where id=@interviewid
Select ul.uniqueid from InterviewMeetings im join UserList ul on im.candidateEmail=ul.EmailId
END
GO