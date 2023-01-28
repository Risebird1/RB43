  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerRisetubeIndexMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerRisetubeIndexMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
Create PROCEDURE [dbo].[usp_GetInterviewerRisetubeIndexMeetings]  
@searchvalue varchar(max)
AS    
Begin       
declare @totalResults int    
      
  --declare @tblTechSkill table (InterviewId int, TechSkillScore float);      
  -- insert into @tblTechSkill      
  -- select InterviewId, sum(WeightageActual) as TechSkillScore from TechSkillReport group by InterviewId
   
    CREATE TABLE #tblTechSkill (InterviewId int, TechSkillScore float)

	insert into #tblTechSkill
	select tsr.Interviewid, sum(tsr.WeightageActual) from TechSkillReport tsr
	group by tsr.Interviewid
      
  declare @tblPatchCount table (InterviewId int, MeetingPatchCount int);      
   insert into @tblPatchCount      
   select InterviewId, count(InterviewId)  from VideoQuestionDetails where NULLIF(VideoLink, '') IS NOT NULL group by InterviewId      
         if(@searchValue is not null)  
   Begin    
    set @totalResults=(select count(a.id) from (select im.id from UnapprovedInterviewerMeetings im       
      inner join [dbo].[InterviewerComments] ic on im.id = ic.InterviewId            
      inner join UserList ulc on ulc.EmailId = im.candidateEmail            
      inner join TransInterviewerListInterviewerSkills cm on ulc.id = cm.UserId            
      inner join InterviewerSkillsChannel IRF on im.ISCId = IRF.InterviewerSkillId                
      inner join InterviewerProfile ca on ca.id = cm.InterviewerProfileId and cm.ISCId = irf.id       
      inner join #tblTechSkill tts on tts.InterviewId = im.id      
      left join @tblPatchCount tpc on tpc.InterviewId=ic.InterviewId      
      where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null  and (im.candidateName like ('%' +@searchValue +'%'))  
	  group by im.id,im.ISCId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail,            
      im.confirmationStatus, im.zoomLink, im.ZoomId, im.interviewer,            
      im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,            
      ulc.MobileNumber, ca.ResumeLink,IRF.id,IRF.JDTiltle,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage  , tpc.MeetingPatchCount,tts.TechSkillScore) a)      
        
 select @totalResults      
 End
 Else
 BEgin
 set @totalResults=(select count(a.id) from (select im.id from UnapprovedInterviewerMeetings im       
      inner join [dbo].[InterviewerComments] ic on im.id = ic.InterviewId            
      inner join UserList ulc on ulc.EmailId = im.candidateEmail            
      inner join TransInterviewerListInterviewerSkills cm on ulc.id = cm.UserId            
      inner join InterviewerSkillsChannel IRF on im.ISCId = IRF.InterviewerSkillId                
      inner join InterviewerProfile ca on ca.id = cm.InterviewerProfileId and cm.ISCId = irf.id       
      inner join #tblTechSkill tts on tts.InterviewId = im.id      
      left join @tblPatchCount tpc on tpc.InterviewId=ic.InterviewId      
      where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null 
      group by im.id,im.ISCId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail,             
      im.confirmationStatus, im.zoomLink, im.ZoomId, im.interviewer,             
      im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,            
      ulc.MobileNumber, ca.ResumeLink,IRF.id,IRF.JDTiltle,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage  , tpc.MeetingPatchCount,tts.TechSkillScore) a)      
        
 select @totalResults      
End 

drop table #tblTechSkill
END
GO
  