/****** Object:  StoredProcedure [dbo].[usp_MyInterviews]    Script Date: 25-09-2019 7.40.51 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_MyInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_MyInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_MyInterviews]               
@emailID varchar(200)                  
AS                
BEGIN          
      
declare @emailIDParam varchar(200)      
set @emailIDParam=@emailID      
      
select im.id, im.IRFId, im.interviewDate, im.interviewSlot, IRF.interviewDuration, ul.EmailId as candidateEmail,       
ISNULL(ca.ClientHRAttemptContact, im.ClientHrContact) as ClientHrContact,              
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,              
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy,        
case         
  when ul.LastName is null then ul.FirstName        
  else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))        
end as candidateName,              
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore, im.HRCoordinatorEmailId,              
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink ,irf.id as irfIntId,irf.ClientId, irf.JDLink,irf.JobLevel,irf.CompanyDetails,               
ic.ApprovalComments, ic.IsApproved,ic.InterviewStatus,ic.IsSelected, ic.MeetingStatus,ic.VideoLink, ulh.MobileNumber as HRMobileNumber,              
cm.id as CandidateMasterId, irf.Heuristicsscore as HeuristicProfileType, ic.VideoDuration, irf.JDTiltle,ca.IsProctor,IRF.AudioComment,Cl.Name as ClientName,IRF.JobCountry,tqc.id as QCPresent,ISNULL(IRF.MeetingPlatform,'Zoom') MeetingPlatform,im.BackUpLink,im.BackUpLinkId 
from InterviewMeetings im                 
inner join IRF  irf on im.IRFId=irf.IRFId left join InterviewComments ic on im.id = ic.InterviewId   
left join Client cl on cl.id=IRF.ClientId  
left join UserList ul on ul.EmailId = im.candidateEmail              
join  CandidatesMaster cm on ul.id=cm.UserId              
join Candidate ca on irf.id=ca.IRFId and cm.id = ca.CandidateMasterId              
left join UserList ulh on ulh.EmailId = im.HRCoordinatorEmailId  
left join Trans_QCFormReport tqc on tqc.Interviewid=im.id
where interviewer =@emailIDParam and confirmationStatus = 1                        
                    
END
GO

