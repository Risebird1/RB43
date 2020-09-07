IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSharedRiseTubeMeetingsForCuratedUser'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSharedRiseTubeMeetingsForCuratedUser
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_GetSharedRiseTubeMeetingsForCuratedUser]   
AS        
Begin         
select distinct im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,    
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,    
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,     
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink, ca.CostEstimate,   
IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,    
ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage       
as ThumbnailImage,(CASE WHEN ic.CandidateAvailable = 1 THEN 'Available' ELSE 'NotAvailable' END)  as CandidateAvailable,  
  tsr.TechSkillScore  
from InterviewMeetings im            
inner join IRF on im.IRFId = IRF.IRFId     
inner join CuratedIRFMapping cim on cim.IRFId = irf.id 
inner join [dbo].[InterviewComments] ic on im.id = ic.InterviewId       
inner join UserList ul on ul.EmailId = im.candidateEmail    
inner join CandidatesMaster cm on cm.UserId = ul.id        
inner join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id    
inner join (select InterviewId, sum(WeightageActual) as TechSkillScore from TechSkillReport group by InterviewId ) tsr  on tsr.InterviewId= im.id
where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null  

END     
GO
           
    
	