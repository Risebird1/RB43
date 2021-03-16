/****** Object:  StoredProcedure [dbo].[usp_GetNotUpdatedMeetingsCount]    Script Date: 15-01-2021 5.08.54 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetNotUpdatedMeetingsCount'))
BEGIN
    DROP PROCEDURE dbo.usp_GetNotUpdatedMeetingsCount
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetNotUpdatedMeetingsCount]   
@currentdate datetime
AS          
Begin         
        
select (select Count(im.id) from InterviewMeetings im  where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, @currentdate)<7 ) as UnassignedInterviewsCount,        
(select Count(im.id) from InterviewMeetings im left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and interviewDate<@currentdate  and ic.MeetingStatus is null and DATEDIFF(DAY, interviewDate, @currentdate)<7 ) as UnfilledMeetingStatusCount,        
(select Count(im.id) from InterviewMeetings im 
inner join InterviewComments ic on ic.InterviewId= im.id 
left join QCFeedbackReport qc on qc.InterviewId = im.id
where confirmationStatus=1 and
(ic.InterviewStatus=2 or ic.InterviewStatus=4 )
and ic.MeetingStatus=4 and (qc.QCStatus = 0 or qc.QCStatus is null)
and DATEDIFF(DAY, interviewDate, @currentdate)<7 ) as UnfilledInterviewStatusCount,        
(select Count(im.id) from InterviewMeetings im  where confirmationStatus=1 and HRCoordinatorEmailId is null and DATEDIFF(DAY, interviewDate, @currentdate)<7 ) as UnfilledCommentsCount        
        
End 
  

GO


