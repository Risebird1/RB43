IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchZohoInterviewForParticipant'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchZohoInterviewForParticipant
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchZohoInterviewForParticipant
AS
BEGIN
Select im.ZoomMeetingId,zm.ParticipantName,zm.Event,zm.ActionDate,zm.Meetingid From InterviewMeetings im left join ZoomMeetingTransmission zm on zm.Meetingid=im.ZoomMeetingId
where im.interviewDate>DateAdd(Minute,310,Getdate()) and im.interviewDate<DateAdd(Minute,330,Getdate())
and im.MeetingPlatForm='Zoho' and ConfirmationStatus=1
END
GO
