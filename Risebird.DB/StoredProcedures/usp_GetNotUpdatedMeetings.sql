/****** Object:  StoredProcedure [dbo].[usp_GetNotUpdatedMeetings]    Script Date: 15-01-2021 4.53.33 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetNotUpdatedMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetNotUpdatedMeetings
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetNotUpdatedMeetings]  
@timeNow datetime
AS  
Begin 
select * from InterviewMeetings im 
left join InterviewComments ic on im.id=ic.InterviewId
where confirmationStatus=1 and ((interviewer is null) or 
(ic.MeetingStatus is null and DATEDIFF(MINUTE, interviewDate, @timeNow)>15)
or (ic.InterviewStatus is null and DATEDIFF(MINUTE, interviewDate, @timeNow)>15) 
or (ic.InterviewerComment is null and DATEDIFF(MINUTE, interviewDate, @timeNow)>15))
order by interviewDate desc


End

GO


