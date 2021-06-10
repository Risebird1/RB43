/****** Object:  StoredProcedure [dbo].[usp_MyQCOpenInterviews]    Script Date: 14-11-2019 7.03.24 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_MyQCOpenInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_MyQCOpenInterviews 
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
CREATE PROCEDURE [dbo].[usp_MyQCOpenInterviews]  
@emailID varchar(200)  
AS  
BEGIN  
select im.id, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,  
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,  
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,  
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,  
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,  
irf.id as irfIntId, irf.JDLink, irf.JDTiltle from InterviewMeetings im  
inner join IRF irf on im.IRFId=irf.IRFId   
inner join InterviewComments ic on ic.InterviewId= im.id  
left join QCFeedbackReport qcf on qcf.InterviewId = im.id  
inner join UserList ul on ul.EmailId  = im.candidateEmail  
inner join CandidatesMaster cm on cm.UserId = ul.id  
inner join Candidate ca on irf.id=ca.IRFId and cm.id = ca.CandidateMasterId  
inner join TransQualityController tqc on tqc.IRFId= irf.id  
inner join UserList uli on uli.id  = tqc.UserId  
where im.confirmationStatus=1 and qcf.QCStatus is null and ic.MeetingStatus=4 and (ic.InterviewStatus=2 or ic.InterviewStatus=4) and im.interviewer!=@emailID and uli.EmailId=@emailID  
  
END  
GO




