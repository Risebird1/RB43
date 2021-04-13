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
@currentdate datetime            
AS                      
Begin                     
                    
select (select Count(im.id) from InterviewMeetings im  where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, @currentdate)<1 ) as UnassignedInterviewsCount,                    
(select Count(im.id) from InterviewMeetings im left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and interviewDate<@currentdate and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, @currentdate)<7 ) as UnfilledMeetingStatusCount,                    
(select Count(im.id) from InterviewMeetings im             
inner join InterviewComments ic on ic.InterviewId= im.id             
left join QCFeedbackReport qc on qc.InterviewId = im.id            
where confirmationStatus=1 and            
(ic.InterviewStatus=2 or ic.InterviewStatus=4 )            
and ic.MeetingStatus=4 and (qc.QCStatus = 0 or qc.QCStatus is null)            
and DATEDIFF(DAY, interviewDate, @currentdate)<7 ) as UnfilledInterviewStatusCount,                    
(select Count(im.id) from InterviewMeetings im  where confirmationStatus=1 and HRCoordinatorEmailId is null and DATEDIFF(DAY, interviewDate, @currentdate)<7 ) as UnfilledCommentsCount,                    
(select Count(im.id) from InterviewMeetings im  where confirmationStatus=1 and (zoomLink is null or zoomLink='') and interviewDate>@currentdate) as MissingZoomLink,    
(Select Count(t1.meetingMember-t2.meetingMember) from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io'  group by meetingid) as t1    
join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io'  group by meetingid) as t2 on t1.Meetingid=t2.Meetingid where (t1.meetingMember-t2.meetingMember)=1) as UnAttendedZoomLink,
(select Count(im.id) from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and interviewDate<@currentdate) as MissingVideoLink
End             
      
      
        

GO
