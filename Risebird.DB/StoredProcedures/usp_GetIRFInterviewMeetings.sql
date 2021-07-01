/****** Object:  StoredProcedure [dbo].[usp_GetIRFInterviewMeetings]    Script Date: 15-10-2019 1.50.19 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFInterviewMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFInterviewMeetings
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetIRFInterviewMeetings]

@id int

AS
Begin  
  
declare    
@IRFId nvarchar(100) = (select IRFId from [dbo].[IRF] where id=@id)    
select irf.PerMintuteRate, irf.TotalBudget,QCF.InterviewId as QCInterview,im.id as imid, im.IRFId, im.interviewDate,  
im.interviewSlot, im.interviewDuration, ul.EmailId as candidateEmail, im.ClientHrContact,  
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,  
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy,  
case 
	when ul.LastName is null then ul.FirstName
	else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))
end as candidateName, 
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore,  
im.JoiningBiometryScore, ca.Phone as MobileNumber, ca.ResumeLink as resumeLink, ic.*, irf.*, qcf.*,  
ul.Isonline as IsOnline, ulh.Isonline as IsHrOnline from InterviewMeetings im    
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId left join irf on IRF.IRFId = im.IRFId    
left join QCFeedbackReport QCF on im.id=QCF.InterviewId  
left join userlist ul on im.candidateEmail = ul.EmailId  
join CandidatesMaster cm on cm.UserId = ul.id  
join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId  
left join userlist ulh on im.ClientHrContact = ulh.EmailId  
  
where im.IRFId = @IRFId   
End  


GO


