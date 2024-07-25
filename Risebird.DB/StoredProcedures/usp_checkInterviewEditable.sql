  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_checkInterviewEditable'))
BEGIN
    DROP PROCEDURE dbo.usp_checkInterviewEditable
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_checkInterviewEditable
@interviewId int
AS
BEGIN
Select ConfirmationStatus From InterviewMeetings where id=@interviewId
END
GO