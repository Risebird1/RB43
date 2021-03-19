  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerRiseTubeMeetingsByJD'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerRiseTubeMeetingsByJD
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
      
        
Create PROCEDURE [dbo].[usp_GetInterviewerRiseTubeMeetingsByJD]            
     @clientid int,        
  @Emailid varchar(100),        
  @JDpattern varchar(100)        
AS            
Begin                  
                
select im.id as imid, im.ISCId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail,             
im.confirmationStatus, im.zoomLink, im.ZoomId, im.interviewer,              
im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, ca.NickName as candidateName,                         
ul.MobileNumber as MobileNumber, ca.ResumeLink as resumeLink,               
IRF.id as irfIntId,IRF.JDTiltle,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage as ThumbnailImage,      
patchvideoCount.MeetingCount as MeetingPatchCount,        
Techscore.TechSkillScore as TechSkillScore
        
from UnapprovedInterviewerMeetings im                  
inner join InterviewerSkillsChannel IRF on im.ISCId = IRF.InterviewerSkillId                
left join [dbo].[InterviewerComments] ic on im.id = ic.InterviewId              
left join UserList ul on ul.EmailId = im.candidateEmail              
join TransInterviewerListInterviewerSkills cm on ul.id = cm.UserId              
join InterviewerProfile ca on ca.id = cm.InterviewerProfileId and cm.ISCId = irf.id         
outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and NULLIF(VideoLink, '') IS NOT NULL)patchvideoCount        
outer apply (select sum(WeightageActual) as TechSkillScore from TechSkillReport where Interviewid=ic.InterviewId )Techscore        
     
where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and IRF.JDTiltle LIKE ('%' +@JDpattern+'%')        
group by im.id,im.ISCId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail,               
im.confirmationStatus, im.zoomLink, im.ZoomId, im.interviewer,              
im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, ca.NickName,                          
ul.MobileNumber, ca.ResumeLink,IRF.id,IRF.JDTiltle,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage  ,patchvideoCount.MeetingCount,Techscore.TechSkillScore 
  
                     
        
End 
GO