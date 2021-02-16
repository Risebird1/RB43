/****** Object:  StoredProcedure [dbo].[usp_GetIAllDateWiseUnApprovedInterviewerMeetings]    Script Date: 16-02-2021 11.24.58 AM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIAllDateWiseUnApprovedInterviewerMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIAllDateWiseUnApprovedInterviewerMeetings
END
GO

SET QUOTED_IDENTIFIER ON
GO

    
      
CREATE PROCEDURE [dbo].[usp_GetIAllDateWiseUnApprovedInterviewerMeetings]        
@startDate datetime,    
@endDate datetime    
AS        
Begin     


select im.id as imid, im.candidateEmail, im.candidateName,im.interviewDate, 
im.interviewDuration, im.HRComment, im.HRCoordinatorEmailId, im.HRImageFileURL, im.confirmationStatus,
im.interviewer, im.interviewSlot, im.ISCId, im.ZoomId, im.zoomLink, ic.MeetingStatus, ic.InterviewStatus, ic.VideoLink, isc.id as iscIntId,
ul.MobileNumber, isc.JDTiltle, Skill.[Name] as PrimarySkill, ipr.CodedResumeLink as encodedresumeLink, ipr.ResumeLink as resumeLink
from UnapprovedInterviewerMeetings im left join InterviewerComments ic on ic.InterviewId = im.id
join UserList ul on ul.EmailId=im.candidateEmail
join InterviewerSkillsChannel isc on isc.InterviewerSkillId = im.ISCId 
join Skill on Skill.id = isc.PrimarySkill
left join InterviewerProfile ipr on ipr.UserId=ul.id
where interviewDate >= @startDate AND interviewDate < @endDate 
order by im.interviewDate desc
       
End         
     
   
GO


