/****** Object:  StoredProcedure [dbo].[usp_GetClientMeetings]    Script Date: 25-09-2019 7.40.51 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetClientMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

 
CREATE PROCEDURE [dbo].[usp_GetClientMeetings]     
@EmailId varchar(300)        
AS      
BEGIN      
select im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore, 
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,
IRF.ClientId,IRF.ClientName,IRF.JDTiltle, IRF.IsBiometricEnabled, IRF.id as irfIntId,ic.MeetingStatus,ic.InterviewStatus,ic.VideoLink,
ic.InterviewerComment,ic.OfferStatus,ic.JoiningStatus,ic.IsApproved,ic.IsSelected, ic.OfferComments,ic.JoiningComments,
ic.ApprovalComments,ic.FinalComments,ic.AadharFileLink, QCF.InterviewId as QCInterview,
cm.id as CandidateId,ul.Isonline as IsOnline,(Select Distinct Isonline from UserList where EmailId=im.ClientHrContact) as IsHrOnline
from InterviewMeetings im 
inner join IRF on im.IRFId = IRF.IRFId 
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId 
left join QCFeedbackReport QCF on im.id=QCF.InterviewId 
left join CandidatesMaster cm on im.candidateEmail = cm.EmailId
inner join TransClientUser tcu on IRF.id=tcu.IRFId
inner join UserList ul on tcu.ClientUserId= ul.id
left join Candidate ca on im.candidateEmail = ca.EmailId and irf.id= ca.IRFId
where ul.EmailId LIKE @EmailId AND ul.IsActive=1        
      
END   
GO


