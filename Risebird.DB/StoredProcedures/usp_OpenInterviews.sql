IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_OpenInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_OpenInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_OpenInterviews] 
@emailID varchar(200),  
@timeNow datetime  
AS    
BEGIN    
select im.id, im.IRFId, im.interviewDate, im.interviewSlot, Case When irf.IsNewSkillChannel=1 then pm.DurationSelectedMin else IRF.interviewDuration end as interviewDuration,im.candidateEmail,   
ISNULL(ca.ClientHRAttemptContact, im.ClientHrContact) as ClientHrContact,      
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,    
im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,    
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,    
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,irf.JDTiltle,irf.id as irfIntId, ul.IsActive as IsInterviewerActive,   
irf.JDLink,tirf.PerSuccessRate as ExpectedPerHourRate,irf.IsNewSkillChannel,
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
where interviewer is null and confirmationStatus = 1 and IsEmergency=0 and ul.EmailId = @emailID and im.interviewDate>=@timeNow and tirf.IsActive=1   and  ul.IsActive=1
END   

GO 