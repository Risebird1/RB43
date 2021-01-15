/****** Object:  StoredProcedure [dbo].[usp_GetNotUpdatedMeetingsCount]    Script Date: 15-01-2021 5.08.54 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetNotUpdatedMeetingsCount'))
BEGIN
    DROP PROCEDURE dbo.usp_GetNotUpdatedMeetingsCount
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetNotUpdatedMeetingsCount]  
@timeNow datetime
AS  
Begin 

select (select Count(im.id) from InterviewMeetings im  where confirmationStatus=1 and interviewer is null) as UnassignedInterviewsCount,
(select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and interviewer is not null and ic.MeetingStatus is null and DATEDIFF(MINUTE, interviewDate, @timeNow)>60) as UnfilledMeetingStatusCount,
(select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and interviewer is not null and ic.InterviewStatus is null and DATEDIFF(MINUTE, interviewDate, @timeNow)>60) as UnfilledInterviewStatusCount,
(select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and interviewer is not null and ic.InterviewerComment is null and DATEDIFF(MINUTE, interviewDate, @timeNow)>60) as UnfilledCommentsCount

End

GO


