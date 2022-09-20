IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetMeetingPlatformByIRFId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetMeetingPlatformByIRFId
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetMeetingPlatformByIRFId
@IRFId varchar(200)
AS
BEGIN
select MeetingPlatform from IRF where IRFId = @IRFId
END
Go

