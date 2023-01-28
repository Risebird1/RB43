  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerRiseTubeMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerRiseTubeMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
                 
CREATE PROCEDURE [dbo].[usp_GetInterviewerRiseTubeMeetings]                
  @startFromParam int,                      
  @endWithParam int,                  
  @searchvalue varchar(max)                  
AS                          
Begin                      
 declare @startFrom INT, @endWith int                    
                       
   set @startFrom = @startFromParam                      
  set @endWith = @endWithParam      
  
--declare @tblTechSkill table (InterviewId int, TechSkillScore float);                      
-- insert into @tblTechSkill                      
-- select InterviewId, sum(WeightageActual) as TechSkillScore from InterviewerTechSkillReport group by InterviewId  
 
CREATE TABLE #tblTechSkill (InterviewId int, TechSkillScore float)

insert into #tblTechSkill
select tsr.Interviewid, sum(tsr.WeightageActual) from TechSkillReport tsr
group by tsr.Interviewid
                      
declare @tblPatchCount table (InterviewId int, MeetingPatchCount int);                      
 insert into @tblPatchCount                      
 select InterviewId, count(InterviewId) as MeetingPatchCount  from VideoQuestionDetails where NULLIF(VideoLink, '') IS NOT NULL group by InterviewId                      
                      
      if(@searchValue is not null)                  
   Begin                  
   ;WITH meetings_rows AS (select ROW_NUMBER() OVER (                      
   ORDER BY im.interviewDate desc                      
   ) row_num, im.id as imid, im.ISCId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail,                            
   im.confirmationStatus, im.zoomLink, im.ZoomId, im.interviewer,         
    im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, ca.NickName as candidateName,                            
   ulc.MobileNumber as MobileNumber, ca.CodedResumeLink as resumeLink,                             
   IRF.id as irfIntId,IRF.JDTiltle,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage as ThumbnailImage,                          
   tpc.MeetingPatchCount,                      
   tts.TechSkillScore,irc.PerHourRateIndia,irc.PerHourRateOutside ,irc.Senioritylevel,ag.AssessmentGroup,isNull(irc.ProfileType,0) as ProfileType,Isnull(irc.CompentencyLevel,0) as  CompentencyLevel,irc.CompanyType,irc.Domain                    
   from UnApprovedInterviewerMeetings im                              
   inner join [dbo].[InterviewerComments] ic on im.id = ic.InterviewId                            
   inner join UserList ulc on ulc.EmailId = im.candidateEmail                            
   inner join TransInterviewerListInterviewerSkills cm on ulc.id = cm.UserId                            
   inner join InterviewerSkillsChannel IRF on im.ISCId = IRF.InterviewerSkillId                                
   inner join InterviewerProfile ca on ca.id = cm.InterviewerProfileId and cm.ISCId = irf.id                       
   inner join #tblTechSkill tts on tts.InterviewId = im.id                      
   left join @tblPatchCount tpc on tpc.InterviewId=ic.InterviewId               
   left join [InterviewerRecommendation] irc on irc.Interviewid=im.id               
   left join AssessmentGroup ag on ag.id=irf.AssessmentGroupId               
   where (ic.InterviewStatus =2) and ic.VideoLink is not null and (ca.NickName like ('%' +@searchValue +'%'))                    
   group by im.id,im.ISCId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail,                            
   im.confirmationStatus, im.zoomLink, im.ZoomId, im.interviewer,                              
    im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, ca.NickName ,                            
   ulc.MobileNumber, ca.CodedResumeLink,IRF.id,IRF.JDTiltle,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage  ,tpc.MeetingPatchCount,tts.TechSkillScore,irc.PerHourRateIndia,irc.PerHourRateOutside ,irc.Senioritylevel,ag.AssessmentGroup,irc.ProfileType,irc.CompentencyLevel,irc.CompanyType,irc.Domain)                  
                     
 select * from meetings_rows --where row_num>@startFrom and row_num<=@endWith              
 End                
 Else                
 BEGIn                
   ;WITH meetings_rows AS (select ROW_NUMBER() OVER (                 
   ORDER BY im.interviewDate desc                      
   ) row_num, im.id as imid, im.ISCId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail,                            
   im.confirmationStatus, im.zoomLink, im.ZoomId, im.interviewer,        
   im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy,ca.NickName as candidateName,                            
   ulc.MobileNumber as MobileNumber, ca.CodedResumeLink as resumeLink,                             
   IRF.id as irfIntId,IRF.JDTiltle,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage as ThumbnailImage,                          
   tpc.MeetingPatchCount,                      
   tts.TechSkillScore ,irc.PerHourRateIndia,irc.PerHourRateOutside ,irc.Senioritylevel ,ag.AssessmentGroup,isNull(irc.ProfileType,0) as ProfileType,Isnull(irc.CompentencyLevel,0) as  CompentencyLevel,irc.CompanyType,irc.Domain        
   from UnApprovedInterviewerMeetings im                              
   inner join [dbo].[InterviewerComments] ic on im.id = ic.InterviewId                            
   inner join UserList ulc on ulc.EmailId = im.candidateEmail                            
   inner join TransInterviewerListInterviewerSkills cm on ulc.id = cm.UserId                            
   inner join InterviewerSkillsChannel IRF on im.ISCId = IRF.InterviewerSkillId                                
   inner join InterviewerProfile ca on ca.id = cm.InterviewerProfileId and cm.ISCId = irf.id                       
   inner join #tblTechSkill tts on tts.InterviewId = im.id                      
   left join @tblPatchCount tpc on tpc.InterviewId=ic.InterviewId                
   left join [InterviewerRecommendation] irc on irc.Interviewid=im.id               
   left join AssessmentGroup ag on ag.id=irf.AssessmentGroupId               
   where (ic.InterviewStatus =2) and ic.VideoLink is not null                     
   group by im.id,im.ISCId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail,                            
   im.confirmationStatus, im.zoomLink, im.ZoomId, im.interviewer,         
   im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, ca.NickName,                            
   ulc.MobileNumber, ca.CodedResumeLink,IRF.id,IRF.JDTiltle,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage  ,tpc.MeetingPatchCount,tts.TechSkillScore,irc.PerHourRateIndia,irc.PerHourRateOutside ,irc.Senioritylevel ,ag.AssessmentGroup,irc.ProfileType,irc.CompentencyLevel,irc.CompanyType,irc.Domain)                      
                     
 select * from meetings_rows --where row_num>@startFrom and row_num<=@endWith                      
 END                
                      
  drop table #tblTechSkill                    
END                                                                             
              
GO
               
                                     
             
    
                         
           
  

  