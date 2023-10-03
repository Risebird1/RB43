IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FecthUpcomingInterviewsRedMarkers'))
BEGIN
    DROP PROCEDURE usp_FecthUpcomingInterviewsRedMarkers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FecthUpcomingInterviewsRedMarkers
@CurrentDate datetime
AS
BEGIN
Select distinct ul.EmailId,ul.uniqueid,ip.id From InterviewerProfile ip join UserList ul on ul.id=ip.Userid 
join InterviewMeetings im on im.interviewer=ul.EmailId 
where interviewDate>@CurrentDate and ip.RedMarker>0 and IsNull(ip.RedmarkerAcknowledge,0)=0
END
Go