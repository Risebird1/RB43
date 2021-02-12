/****** Object:  StoredProcedure [dbo].[usp_GetUnapprovedInterviewerScheduledInterviews]    Script Date: 25-05-2020 10.49.02 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetUnapprovedInterviewerScheduledInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUnapprovedInterviewerScheduledInterviews
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

     
CREATE PROCEDURE [dbo].[usp_GetUnapprovedInterviewerScheduledInterviews]         
@emailID varchar(200)            
AS          
BEGIN          
select DISTINCT im.*, isc.id as iscIntId, isc.JDTiltle,isc.CalendlyLink, isc.EndDate, ic.VideoLink, ul.MobileNumber as InterviewerPhone, Skill.[Name] as PrimarySkill   
from UnapprovedInterviewerMeetings im       
inner join InterviewerSkillsChannel isc on im.ISCId=isc.InterviewerSkillId    
left join InterviewerComments ic on im.id = ic.InterviewId      
join UserList ul on im.interviewer = ul.EmailId     
left join Skill on isc.PrimarySkill = Skill.id    
where im.candidateEmail =@emailID and confirmationStatus = 1     
        
END       
  
  
GO


