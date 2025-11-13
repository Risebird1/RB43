IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveZohoInterviewParticipantDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveZohoInterviewParticipantDetails
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_SaveZohoInterviewParticipantDetails
@Event varchar(100),
@ParticipantName varchar(200),
@Meetingid varchar(100),
@ActionDate Datetime
AS
BEGIN
Insert into ZoomMeetingTransmission([Event],ParticipantName,Meetingid,ActionDate) values(@Event,@ParticipantName,@Meetingid,@ActionDate)
END
GO