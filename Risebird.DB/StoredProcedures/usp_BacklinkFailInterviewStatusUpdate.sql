  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_BacklinkFailInterviewStatusUpdate'))
BEGIN
    DROP PROCEDURE usp_BacklinkFailInterviewStatusUpdate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_BacklinkFailInterviewStatusUpdate
@interviewid int   
AS  
BEGIN  
Update InterviewComments set MeetingStatus=3 where interviewid=@interviewid
Exec usp_InsertOrUpdateInterviewPoints @interviewid
END  
GO
