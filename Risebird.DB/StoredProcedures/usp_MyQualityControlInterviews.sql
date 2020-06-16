/****** Object:  StoredProcedure [dbo].[usp_MyQualityControlInterviews]    Script Date: 13-11-2019 3.00.19 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_MyQualityControlInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_MyQualityControlInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_MyQualityControlInterviews]     
@emailID varchar(200)        
AS      
BEGIN      
select qcfr.*,im.id, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,
irf.id as irfIntId,irf.ClientId, irf.JDLink,irf.JobLevel, ic.ApprovalComments, ic.IsApproved,ic.InterviewStatus,
ic.IsSelected, ic.MeetingStatus,ic.VideoLink from InterviewMeetings im 
inner join IRF  irf on im.IRFId=irf.IRFId 
left join InterviewComments ic on im.id = ic.InterviewId
left join QCFeedbackReport qcfr on qcfr.InterviewId = im.id    
left join Candidate ca on ca.EmailId = im.candidateEmail and ca.IRFId=irf.id
where QCInterviewer = @emailID
END   
GO


