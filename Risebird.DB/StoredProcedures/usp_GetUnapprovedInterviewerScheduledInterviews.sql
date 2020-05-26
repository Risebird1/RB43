/****** Object:  StoredProcedure [dbo].[usp_GetUnapprovedInterviewerScheduledInterviews]    Script Date: 25-05-2020 10.49.02 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetUnapprovedInterviewerScheduledInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUnapprovedInterviewerScheduledInterviews
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


 
Create PROCEDURE [dbo].[usp_GetUnapprovedInterviewerScheduledInterviews]     
@emailID varchar(200)        
AS      
BEGIN      
select InterviewMeetings.*, isc.id as iscIntId, isc.JDTiltle,isc.CalendlyLink, isc.EndDate, ic.VideoLink, ul.MobileNumber as InterviewerPhone, Skill.[Name] as PrimarySkill from InterviewMeetings    
inner join InterviewerSkillsChannel isc on InterviewMeetings.IRFId=isc.InterviewerSkillId
left join InterviewComments ic on InterviewMeetings.id = ic.InterviewId  
join UserList ul on InterviewMeetings.interviewer = ul.EmailId 
left join Skill on isc.PrimarySkill = Skill.id
where interviewMeetings.candidateEmail =@emailID and confirmationStatus = 1         
    
END   
GO


