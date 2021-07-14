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
@EmailId varchar(300),  
@timeNow datetime  
AS        
BEGIN            
 declare @profileId int, @roleId int, @userId int, @EmailIdParam varchar(300), @timeNowParam datetime;      
  set @EmailIdParam = @EmailId      
  set @timeNowParam=@timeNow      
  set @profileId = (select id from [Profile] where [Name]='Client')      
  set @userId  = (select id from UserList where EmailId=@EmailIdParam and idProfile=@profileId)      
  set @roleId  = (select RoleId from ClientUser where UserId=@userId)      
        
      
if(@roleId=1)      
      
 Begin      
  select distinct im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId as candidateEmail, 
  @EmailIdParam as ClientHrContact,      
  im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,      
  im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy,    
  case     
  when ulc.LastName is null then ulc.FirstName    
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))    
  end as candidateName,      
  im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,       
  ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,      
  IRF.ClientId,IRF.ClientName,IRF.JDTiltle, IRF.IsBiometricEnabled, IRF.id as irfIntId,ic.MeetingStatus,ic.InterviewStatus,ic.VideoLink,      
  ic.InterviewerComment,ic.OfferStatus,ic.JoiningStatus,ic.IsApproved,ic.IsSelected, ic.OfferComments,ic.JoiningComments,      
  ic.ApprovalComments,ic.FinalComments,ic.AadharFileLink,ic.MoveToCP, QCF.InterviewId as QCInterview, QCF.QCStatus,      
  cm.id as CandidateId,ulc.Isonline as IsOnline,ul.Isonline as IsHrOnline, ca.ClientHRAttemptContact as CandidateClientHR      
  from InterviewMeetings im       
  inner join IRF on im.IRFId = IRF.IRFId       
  left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId       
  left join QCFeedbackReport QCF on im.id=QCF.InterviewId       
  left join UserList ulc on im.candidateEmail= ulc.EmailId      
  join CandidatesMaster cm on ulc.id= cm.UserId      
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId      
  inner join TransClientUser tcu on IRF.id=tcu.IRFId      
  left join UserList ul on tcu.ClientUserId= ul.id      
      
  where ul.EmailId LIKE @EmailIdParam AND ul.IsActive=1 and (im.ClientHrContact=@EmailIdParam or ca.ClientHRAttemptContact=@EmailIdParam)      
  and irf.IsDeleted=0      
  --and ((irf.IsDeleted = 1 and cast(interviewDate as date) >= cast(DATEADD(day, -30, @timeNowParam) as date)) or IRF.IsDeleted=0)         
      
      
 End      
      
else if(@roleId=4)      
 Begin      
  select im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId as candidateEmail, im.ClientHrContact,      
  im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,      
  im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy,     
  case     
  when ulc.LastName is null then ulc.FirstName    
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))    
  end as candidateName,      
  im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,       
  ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,      
  IRF.ClientId,IRF.ClientName,IRF.JDTiltle, IRF.IsBiometricEnabled, IRF.id as irfIntId,ic.MeetingStatus,ic.InterviewStatus,ic.VideoLink,      
  ic.InterviewerComment,ic.OfferStatus,ic.JoiningStatus,ic.IsApproved,ic.IsSelected, ic.OfferComments,ic.JoiningComments,      
  ic.ApprovalComments,ic.FinalComments,ic.AadharFileLink,ic.MoveToCP, QCF.InterviewId as QCInterview, QCF.QCStatus,      
  cm.id as CandidateId,ulc.Isonline as IsOnline,ul.Isonline as IsHrOnline, ca.ClientHRAttemptContact as CandidateClientHR      
  from InterviewMeetings im       
 inner join IRF on im.IRFId = IRF.IRFId       
  left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId       
  left join QCFeedbackReport QCF on im.id=QCF.InterviewId       
  left join UserList ulc on im.candidateEmail= ulc.EmailId      
  join CandidatesMaster cm on ulc.id= cm.UserId      
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId      
  inner join TransClientUser tcu on IRF.id=tcu.IRFId      
  left join UserList ul on tcu.ClientUserId= ul.id      
      
  where ul.EmailId LIKE @EmailIdParam AND ul.IsActive=1 and im.staffingAgency=@EmailIdParam      
  and irf.IsDeleted=0      
  --and ((irf.IsDeleted = 1 and cast(interviewDate as date) >= cast(DATEADD(day, -30, @timeNowParam) as date)) or IRF.IsDeleted=0)         
      
 End      
      
else      
 Begin      
  select im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId as candidateEmail, im.ClientHrContact,      
  im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,      
  im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy,     
  case     
  when ulc.LastName is null then ulc.FirstName    
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))    
  end as candidateName,      
  im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,       
  ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,      
  IRF.ClientId,IRF.ClientName,IRF.JDTiltle, IRF.IsBiometricEnabled, IRF.id as irfIntId,ic.MeetingStatus,ic.InterviewStatus,ic.VideoLink,      
  ic.InterviewerComment,ic.OfferStatus,ic.JoiningStatus,ic.IsApproved,ic.IsSelected, ic.OfferComments,ic.JoiningComments,      
  ic.ApprovalComments,ic.FinalComments,ic.AadharFileLink,ic.MoveToCP, QCF.InterviewId as QCInterview, QCF.QCStatus,      
  cm.id as CandidateId,ulc.Isonline as IsOnline,ul.Isonline as IsHrOnline, ca.ClientHRAttemptContact as CandidateClientHR      
  from InterviewMeetings im       
  inner join IRF on im.IRFId = IRF.IRFId       
  left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId       
  left join QCFeedbackReport QCF on im.id=QCF.InterviewId       
  left join UserList ulc on im.candidateEmail= ulc.EmailId      
  join CandidatesMaster cm on ulc.id= cm.UserId      
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId      
  inner join TransClientUser tcu on IRF.id=tcu.IRFId      
  left join UserList ul on tcu.ClientUserId= ul.id      
      
  where ul.EmailId LIKE @EmailIdParam AND ul.IsActive=1      
  and irf.IsDeleted=0      
  --and ((irf.IsDeleted = 1 and cast(interviewDate as date) >= cast(DATEADD(day, -30, @timeNowParam) as date)) or IRF.IsDeleted=0)         
      
 End      
            
END    
GO


