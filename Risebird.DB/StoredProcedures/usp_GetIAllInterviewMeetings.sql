/****** Object:  StoredProcedure [dbo].[usp_GetIAllInterviewMeetings]    Script Date: 05-11-2019 6.03.39 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetIAllInterviewMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIAllInterviewMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
    
   
Create PROCEDURE [dbo].[usp_GetIAllInterviewMeetings]    
    
AS    
Begin    
    
select Meetings.*, ca.Phone as MobileNumber, ca.ResumeLink as resumeLink from (select im.id imid,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail,  
im.ClientHrContact, im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice,   
im.PreferredLocation, im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.candidateName, im.currentEmployerName,   
im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,  
IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, ic.AadharFileLink, cm.id as CandidateId,  
(Select distinct Isonline from UserList where Emailid=im.candidateEmail) as IsOnline,(Select distinct Isonline from UserList where Emailid=im.ClientHrContact) as IsHrOnline,(Select distinct Isonline from UserList where Emailid=im.interviewer) as IsInterviewerOnline,cm.id as CandidateMasterid from InterviewMeetings im      
inner join IRF on im.IRFId = IRF.IRFId    
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId
Left join UserList ul on ul.EmailId=im.candidateEmail
left join CandidatesMaster cm on ul.id = cm.Userid) Meetings  
left join Candidate ca on Meetings.CandidateMasterid = ca.CandidateMasterId and Meetings.irfIntId= ca.IRFId  

   
End    
   



   
GO

