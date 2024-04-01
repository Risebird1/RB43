IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCalibrationCallDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCalibrationCallDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetCalibrationCallDetails
@irfid int
as
Begin
Select * From CalibrationMeetings where Irfid=@irfid
end
GO