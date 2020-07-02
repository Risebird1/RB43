IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_OpenInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_OpenInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_OpenInterviews] 
   @emailID varchar(200)     
AS
BEGIN
select distinct im.id, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,
ulc.MobileNumber, ca.ResumeLink as resumeLink, 
irf.JDLink from InterviewMeetings im  
inner join IRF irf on im.IRFId=irf.IRFId   
inner join [dbo].[TransInterviewer] tirf on IRF.id =tirf.IRFId  
inner join [dbo].[Interviewer] inte on tirf.InterviewerId = inte.id  
inner join [dbo].[UserList] ul on inte.UserId = ul.id  
left join UserList ulc on ulc.EmailId = im.candidateEmail
join CandidatesMaster cm on cm.UserId = ulc.id
join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId=irf.id
where interviewer is null and confirmationStatus = 1 and IsEmergency=0 and ul.EmailId = @emailID
END
GO 