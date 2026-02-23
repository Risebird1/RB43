IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveCalibrationMeetingNotesById'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveCalibrationMeetingNotesById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_SaveCalibrationMeetingNotesById
@id int,
@Notes varchar(max)
AS
BEGIN
Update CalibrationMeetings set Notes=ISNULL(@Notes,'') where id=@id
END
GO