/****** Object:  StoredProcedure [dbo].[usp_UnAssignQCInterviewer]    Script Date: 14-11-2019 2.29.59 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UnAssignQCInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_UnAssignQCInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UnAssignQCInterviewer] @id int
AS
Begin Try
  
	Declare @meetingId int

	Set @meetingId = @id
	
	IF EXISTS (SELECT 1 FROM InterviewMeetings where Id = @meetingId) 
		BEGIN
		    UPDATE InterviewMeetings Set QCInterviewer = null where id = @meetingId
		END
 
  End Try 

  Begin Catch
	RAISERROR ('Error While updating Interviwer', -- Message text.
			16, -- Severity.
			1 -- State.
			);
  End Catch




GO


