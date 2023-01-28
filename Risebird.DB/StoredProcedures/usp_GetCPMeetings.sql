/****** Object:  StoredProcedure [dbo].[usp_GetCPMeetings]    Script Date: 4/12/2020 5:28:28 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCPMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCPMeetings
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
    
    
CREATE PROCEDURE [dbo].[usp_GetCPMeetings]        
     @clientid int 
AS        
Begin            
  
	CREATE TABLE #PatchVideoCountTbl (InterviewId int, VideoCount int)

	insert into #PatchVideoCountTbl
	select vqd.Interviewid, count(vqd.id) from VideoQuestionDetails vqd
	join InterviewComments ic on ic.InterviewId = vqd.Interviewid
	where NULLIF(vqd.VideoLink, '') IS NOT NULL
	group by vqd.Interviewid

	CREATE TABLE #TechSkillScoreTbl (InterviewId int, WeightageActual float)

	insert into #TechSkillScoreTbl
	select tsr.Interviewid, sum(tsr.WeightageActual) from TechSkillReport tsr
	join InterviewComments ic on ic.InterviewId = tsr.Interviewid
	group by tsr.Interviewid


select distinct im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId as candidateEmail, im.ClientHrContact,              
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,im.InterviewBiometryScore,              
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy,   
case   
  when ulc.LastName is null then ulc.FirstName  
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))  
end as candidateName,              
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,               
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,               
IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage as ThumbnailImage,QCF.InterviewId as QCInterview,(CASE WHEN ic.CandidateAvailable = 1 THEN 'Available' ELSE 'NotAvailable' END) as CandidateAvailable,            
patchvideoCount.VideoCount as MeetingPatchCount,        
Techscore.WeightageActual as TechSkillScore,ic.IsLock,ucl.EmailId, ic.ThumbnailTimeSeconds          
from InterviewMeetings im                
inner join [dbo].[InterviewComments] ic on im.id = ic.InterviewId              
inner join UserList ulc on ulc.EmailId = im.candidateEmail              
inner join CandidatesMaster cm on ulc.id = cm.UserId              
inner join QCFeedbackReport QCF on im.id=QCF.InterviewId              
inner join IRF on im.IRFId = IRF.IRFId                  
inner join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id              
inner join dbo.UserList ul on ul.id = tc.ClientUserId              
inner join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id      
left join UserList ucl on ucl.id=ic.ClientUserid 
left join #PatchVideoCountTbl as patchvideoCount on patchvideoCount.InterviewId = ic.InterviewId
left join #TechSkillScoreTbl as Techscore on Techscore.InterviewId = ic.InterviewId
--outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and NULLIF(VideoLink, '') IS NOT NULL)patchvideoCount        
--outer apply (select sum(WeightageActual) as TechSkillScore from TechSkillReport where Interviewid=ic.InterviewId )Techscore        
where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and ic.MoveToCP =1 and IRF.ClientId=@clientid      
group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId, im.ClientHrContact,              
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,              
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, ulc.FirstName, ulc.LastName,              
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,               
ca.Phone, ca.ResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage  , QCF.InterviewId ,ic.CandidateAvailable,
  
    
patchvideoCount.VideoCount,Techscore.WeightageActual,ic.IsLock,ucl.EmailId, ic.ThumbnailTimeSeconds        
     
	 
	 drop table #PatchVideoCountTbl
	 drop table #TechSkillScoreTbl

End           
 

GO
