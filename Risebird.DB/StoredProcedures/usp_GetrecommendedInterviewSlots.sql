IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetrecommendedInterviewSlots'))
BEGIN
    DROP PROCEDURE dbo.usp_GetrecommendedInterviewSlots
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetrecommendedInterviewSlots
(@slotsData AS [dbo].[RecommendedSlots_TableType] READONLY)
AS
BEGIN
Select sd.SlotTime,Count(im.id) interviewcount FRom @slotsData sd left join InterviewMeetings im on sd.SlotTime=im.interviewDate and im.confirmationStatus=1
group  by sd.SlotTime
END
Go