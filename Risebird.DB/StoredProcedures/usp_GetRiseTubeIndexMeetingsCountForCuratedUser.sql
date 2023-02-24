/****** Object:  StoredProcedure [dbo].[usp_GetRiseTubeIndexMeetingsCountForCuratedUser]    Script Date: 12-11-2020 6.20.32 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRiseTubeIndexMeetingsCountForCuratedUser'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRiseTubeIndexMeetingsCountForCuratedUser
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
CREATE PROCEDURE [dbo].[usp_GetRiseTubeIndexMeetingsCountForCuratedUser]    
  @Emailid varchar(100),      
  @searchValue varchar(max) = null  
AS    
Begin   
  
DECLARE @id int   
Declare @Role varchar(50)  
  Select @id=id from UserList where EmailId=@Emailid  
  Select @Role=pr.Name from UserList ul join Profile pr on ul.idProfile=pr.id where Emailid= @Emailid  
if(@Role='SeniorHR' or @Role='SupportAdmin') 
Begin
if(@searchValue is not null)  
  Begin  
   Select Count(im.id) from InterviewMeetings im            
   inner join IRF on im.IRFId = IRF.IRFId          
   left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId        
   left join UserList ul on ul.EmailId = im.candidateEmail        
   join CandidatesMaster cm on ul.id = cm.UserId        
   join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id        
   left join QCFeedbackReport QCF on im.id=QCF.InterviewId        
   inner join client cl on IRF.ClientID=cl.id   
   join SeniorHR sh on sh.Clientid=IRF.ClientId  
   left join Userlist ulsh on ulsh.id=sh.Userid  
   where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and (im.candidateName like ('%' +@searchValue +'%') or irf.JDTiltle like ('%' +@searchValue +'%')) and ulsh.EmailId=@Emailid    
  End  
 else  
  Begin  
   Select Count(im.id) from InterviewMeetings im            
   inner join IRF on im.IRFId = IRF.IRFId          
   left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId        
   left join UserList ul on ul.EmailId = im.candidateEmail        
   join CandidatesMaster cm on ul.id = cm.UserId        
   join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id        
   left join QCFeedbackReport QCF on im.id=QCF.InterviewId        
   inner join client cl on IRF.ClientID=cl.id 
   join SeniorHR sh on sh.Clientid=IRF.ClientId  
   left join Userlist ulsh on ulsh.id=sh.Userid  
   where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and ulsh.EmailId=@Emailid    
     End  
END
if(@searchValue is not null)  
  
 Begin  
  
  select count(a.id) from (select im.id  
    from InterviewMeetings im              
    inner join IRF on im.IRFId = IRF.IRFId            
    left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId         
    left join UserList ul on ul.EmailId = im.candidateEmail      
    join CandidatesMaster cm on cm.UserId = ul.id          
    join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id      
    --join CandidateCuratedScore ccs on ul.EmailId= ccs.CandidateEmailId      
    left join QCFeedbackReport QCF on im.id=QCF.InterviewId          
    inner join client cl on IRF.ClientID=cl.id          
    inner join CuratedIRFMapping cim on cim.IRFId = irf.id      
    inner join CuratedGroupSubscribers cgs on cgs.CuratedGroupId = cim.CuratedGroupId      
    outer apply (Select top 1 status from SuitableCandidate where InterviewId= ic.InterviewId and Userid=@id order by InterstedOn desc)suit  
    outer apply (Select top 1 status from RiseTubeVedioShortList where InterviewId= ic.InterviewId and Userid=@id  order by ShortListedOn desc)rtvsl  
    outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and NULLIF(VideoLink, '') IS NOT NULL)patchvideoCount  
    outer apply (select sum(WeightageActual) as TechSkillScore from TechSkillReport where Interviewid=ic.InterviewId )Techscore  
    where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and cgs.UserId=@id  --and ccs.CuratedEmailId=@Emailid and ccs.Score>=30 
	and (im.candidateName like ('%' +@searchValue +'%') or irf.JDTiltle like ('%' +@searchValue +'%'))
   
    group by im.id, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,      
    im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,  
    im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,      
    im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.JoiningBiometryScore,       
    ca.Phone, ca.ResumeLink ,  ca.CostEstimate,IRF.id ,IRF.JDLink ,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,      
    ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id ,ic.ThumbnailImage         
    , QCF.InterviewId ,suit.Status ,rtvsl.Status ,ic.CandidateAvailable,  
    patchvideoCount.MeetingCount,Techscore.TechSkillScore, ic.ThumbnailTimeSeconds) a  
  
 End  
  
Else  
 Begin  
  select count(a.id) from (select im.id  
    from InterviewMeetings im              
    inner join IRF on im.IRFId = IRF.IRFId            
    left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId         
    left join UserList ul on ul.EmailId = im.candidateEmail      
    join CandidatesMaster cm on cm.UserId = ul.id          
    join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id      
    --join CandidateCuratedScore ccs on ul.EmailId= ccs.CandidateEmailId      
    left join QCFeedbackReport QCF on im.id=QCF.InterviewId          
    inner join client cl on IRF.ClientID=cl.id          
    inner join CuratedIRFMapping cim on cim.IRFId = irf.id      
    inner join CuratedGroupSubscribers cgs on cgs.CuratedGroupId = cim.CuratedGroupId      
    outer apply (Select top 1 status from SuitableCandidate where InterviewId= ic.InterviewId and Userid=@id order by InterstedOn desc)suit  
    outer apply (Select top 1 status from RiseTubeVedioShortList where InterviewId= ic.InterviewId and Userid=@id  order by ShortListedOn desc)rtvsl  
    outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and NULLIF(VideoLink, '') IS NOT NULL)patchvideoCount  
    outer apply (select sum(WeightageActual) as TechSkillScore from TechSkillReport where Interviewid=ic.InterviewId )Techscore  
    where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and cgs.UserId=@id  --and ccs.CuratedEmailId=@Emailid and ccs.Score>=30  
    group by im.id, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,      
    im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,  
    im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,      
    im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.JoiningBiometryScore,       
    ca.Phone, ca.ResumeLink ,  ca.CostEstimate,IRF.id ,IRF.JDLink ,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,      
    ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id ,ic.ThumbnailImage         
    , QCF.InterviewId ,suit.Status ,rtvsl.Status ,ic.CandidateAvailable,patchvideoCount.MeetingCount,  
    Techscore.TechSkillScore, ic.ThumbnailTimeSeconds) a  
 End  
  
  
End  
  

GO


