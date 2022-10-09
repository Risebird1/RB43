IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetRecordingDataToUpdate'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRecordingDataToUpdate
END

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_GetRecordingDataToUpdate  
@timeNow datetime  
As  
BEGIN 

Select id, RecordingSid From CallRecordingData 
where DateCreated > DATEADD(MINUTE, -25, @timeNow) 
and Duration is null

END  
GO

 