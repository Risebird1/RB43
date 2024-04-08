IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetFutureDateInterviewSchedule'))
BEGIN
    DROP PROCEDURE dbo.usp_GetFutureDateInterviewSchedule
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetFutureDateInterviewSchedule]     
@id int
AS
Begin
Select im.interviewer,im.interviewDate,im.confirmationStatus from InterviewMeetings im where im.id=@id

END  
GO