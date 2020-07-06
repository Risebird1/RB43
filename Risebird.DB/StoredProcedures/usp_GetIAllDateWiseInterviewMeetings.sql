/****** Object:  StoredProcedure [dbo].[usp_GetIAllInterviewMeetings]    Script Date: 05-11-2019 6.03.39 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetIAllDateWiseInterviewMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIAllDateWiseInterviewMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
    
      
CREATE PROCEDURE [dbo].[usp_GetIAllDateWiseInterviewMeetings]        
@startDate datetime,    
@endDate datetime    
AS        
Begin        
        
select distinct Meetings.*,  ca.ResumeLink as resumeLink, ca.Phone as MobileNumber from (select distinct im.id imid,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail,      
im.ClientHrContact, im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice,       
im.PreferredLocation, im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.candidateName, im.currentEmployerName,       
im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,      
IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,
ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, ic.AadharFileLink, cm.id as CandidateId     
,ulc.Isonline as IsOnline,ulh.Isonline as IsHrOnline,uli.Isonline  as IsInterviewerOnline, im.HRCoordinatorEmailId, im.HRComment
from     
InterviewMeetings im          
inner join IRF on im.IRFId = IRF.IRFId      
left join UserList ulc on im.candidateEmail= ulc.EmailId    
left join UserList ulh on im.ClientHrContact= ulh.EmailId    
left join UserList uli on im.interviewer= uli.EmailId    
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId      
join CandidatesMaster cm on ulc.id = cm.UserId) Meetings      
join Candidate ca on Meetings.CandidateId = ca.CandidateMasterId and Meetings.irfIntId= ca.IRFId      
where interviewDate >= @startDate AND interviewDate < @endDate    
   
    
       
End       
     
   
GO

