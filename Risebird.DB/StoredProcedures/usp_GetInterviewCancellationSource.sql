  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewCancellationSource'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewCancellationSource
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetInterviewCancellationSource
@interviewId int
AS
BEGIN
Select Cancellationsource From InterviewMeetings where id=@interviewId and confirmationStatus=2
END
GO