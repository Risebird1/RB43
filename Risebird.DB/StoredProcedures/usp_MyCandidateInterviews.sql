/****** Object:  StoredProcedure [dbo].[usp_MyCandidateInterviews]    Script Date: 27-11-2019 7.28.59 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_MyCandidateInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_MyCandidateInterviews
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

 
CREATE PROCEDURE [dbo].[usp_MyCandidateInterviews]     
@emailID varchar(200)        
AS      
BEGIN      
select im.id, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore, im.HRCoordinatorEmailId,
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,
irf.id as irfIntId,irf.ClientId, irf.JDLink,irf.JobLevel,irf.CalendlyLink, irf.EndDate, irf.JDTiltle,
ul.MobileNumber as InterviewerPhone, irf.ClientName, Client.[Name] as ClientCompName, ulh.MobileNumber as HRMobileNumber,
(Select Isonline from UserList where EmailId=im.interviewer) as IsOnilne
from InterviewMeetings im  
inner join IRF  irf on im.IRFId=irf.IRFId   
left join UserList ul on im.interviewer = ul.EmailId join Client on Client.id=irf.ClientId  
left join UserList ulc on ulc.EmailId = im.candidateEmail
join CandidatesMaster cm on cm.UserId = ulc.id
join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId
left join UserList ulh on ulh.EmailId=im.HRCoordinatorEmailId
where im.candidateEmail =@emailID and confirmationStatus = 1 
      
END   
GO


