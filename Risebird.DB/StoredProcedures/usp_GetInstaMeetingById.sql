IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInstaMeetingById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInstaMeetingById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetInstaMeetingById]  
@interviewId int  
AS  
Begin  
  
SELECT * FROM InstaInterviewMeetings
WHERE id = @interviewId  
  
End  
  