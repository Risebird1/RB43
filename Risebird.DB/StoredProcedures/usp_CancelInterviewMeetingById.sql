IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CancelInterviewMeetingById'))
BEGIN
    DROP PROCEDURE dbo.usp_CancelInterviewMeetingById
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_CancelInterviewMeetingById]
    @meetingid INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.InterviewMeetings
    SET confirmationStatus = 2
    WHERE id = @meetingid;
END
GO
