IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateRecordingDataFromTwilio'))
BEGIN
    DROP PROCEDURE [dbo].[usp_UpdateRecordingDataFromTwilio]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_UpdateRecordingDataFromTwilio
@id int,
@RecordingSid varchar(1000),
@StartTime datetime,
@Status varchar(20),
@Price varchar(10),
@PriceUnit varchar(10),
@Duration varchar(10),
@RecordingUri varchar(1000)


AS
BEGIN


Update CallRecordingData 
set StartTime=@StartTime, [Status] = @Status, Price = @Price, PriceUnit = @PriceUnit,
Duration = @Duration, RecordingUri = @RecordingUri
where id=@id and RecordingSid = @RecordingSid

END