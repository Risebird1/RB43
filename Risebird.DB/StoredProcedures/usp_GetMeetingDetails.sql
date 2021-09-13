/****** Object:  StoredProcedure [dbo].[usp_GetMeetingDetails]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetMeetingDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetMeetingDetails
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetMeetingDetails]  
@meetingId int
AS  
Begin

	select candidateEmail, interviewDate from InterviewMeetings where id =@meetingId

End
GO


