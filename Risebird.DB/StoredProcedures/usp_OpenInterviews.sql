IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_OpenInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_OpenInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_OpenInterviews]         
@emailID varchar(200),          
@timeNow datetime          
AS            
BEGIN            
select im.id, im.IRFId, im.interviewDate, im.interviewSlot, Case When irf.IsNewSkillChannel=1 then (pm.DurationSelectedMin+5) else IRF.interviewDuration end as interviewDuration,im.candidateEmail,           
ISNULL(ca.ClientHRAttemptContact, im.ClientHrContact) as ClientHrContact,              
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,            
im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,            
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,            
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,irf.id as irfIntId, ISNULL(ul.IsInterviewerAccess, 0) as IsInterviewerActive,  
ISNULL(irf.DmIvLockNew, 0) as DmIvLockNew,
ISNULL(irf.DmDefaultInterviewCap, 20) as DmDefaultInterviewCap,
ISNULL(irf.DmAssignmentPolicy, 'free') as DmAssignmentPolicy,
tirf.InterviewCapacity as InterviewCapacity,
(SELECT COUNT(im2.id) FROM InterviewMeetings im2
 WHERE im2.IRFId = irf.IRFID AND im2.interviewer = @emailID AND im2.interviewer IS NOT NULL
   AND im2.interviewDate > @timeNow AND ISNULL(im2.IsDeleted, 0) = 0
   AND ISNULL(im2.isAutoCancelled, 0) = 0
   AND CAST(im2.confirmationStatus AS varchar(20)) NOT IN ('2', 'Cancelled')) as IvFutureAssignedCount,
  
         
irf.JDLink,IRF.IsVideoRequired,tirf.PerSuccessRate as ExpectedPerHourRate,irf.IsNewSkillChannel,Cl.Name as ClientName, irf.AudioComment,irf.CompanyDetails,       
Case When tirf.Surcharge>0 and irf.IsNewSkillChannel=1 then tirf.Surcharge when tirf.Surcharge=0 and irf.IsNewSkillChannel=1 then im.Surcharge Else 0 end as Surcharge from InterviewMeetings im              
inner join IRF irf on im.IRFId=irf.IRFId        
join Client cl on cl.id=irf.Clientid        
inner join [dbo].[TransInterviewer] tirf on IRF.id =tirf.IRFId              
inner join [dbo].[InterviewerProfile] inte on tirf.InterviewerId = inte.id          
inner join [dbo].[UserList] ul on inte.UserId = ul.id           
left join UserList ulc on ulc.EmailId = im.candidateEmail            
join CandidatesMaster cm on cm.UserId = ulc.id            
join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId=irf.id            
left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.PlanTypeEnumValue=IRF.Heuristicsscore and pm.CompanyType=IRF.CompanyType        
where interviewer is null and confirmationStatus = 1 and IsEmergency=0 and ul.EmailId = @emailID and im.interviewDate>=@timeNow and tirf.IsActive=1 and ISNULL(ul.IsInterviewerAccess, 0)=1 and ISNULL(inte.IsDeleted,0)=0      
END      
     

GO 