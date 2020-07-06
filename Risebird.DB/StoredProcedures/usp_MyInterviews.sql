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
select im.id, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink ,irf.id as irfIntId,irf.ClientId, irf.JDLink,irf.JobLevel,irf.CompanyDetails, ic.ApprovalComments, ic.IsApproved,ic.InterviewStatus,ic.IsSelected, ic.MeetingStatus,ic.VideoLink, cm.id as CandidateMasterId from InterviewMeetings im   
inner join IRF  irf on im.IRFId=irf.IRFId left join InterviewComments ic on im.id = ic.InterviewId    
left join UserList ul on ul.EmailId = im.candidateEmail
join  CandidatesMaster cm on ul.id=cm.UserId
join Candidate ca on irf.id=ca.IRFId and cm.id = ca.CandidateMasterId
where interviewer =@emailID and confirmationStatus = 1          
      
END   
GO


