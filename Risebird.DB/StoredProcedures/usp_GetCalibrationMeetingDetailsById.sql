IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCalibrationMeetingDetailsById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCalibrationMeetingDetailsById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetCalibrationMeetingDetailsById  
@id int  
AS  
BEGIN  
Select Notes,TranscriptLink From CalibrationMeetings where id=@id  
END
GO
