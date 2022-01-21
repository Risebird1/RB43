/****** Object:  StoredProcedure [dbo].[usp_GetClientSelectedMeetings]    Script Date: 25-09-2019 7.40.51 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetClientSelectedMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientSelectedMeetings 
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

 
  
   
Create PROCEDURE [dbo].[usp_GetClientSelectedMeetings]   
@EmailId varchar(300),  
@timeNow datetime = null,  
@startDate datetime = null,  
@endDate datetime=null  
AS        
BEGIN            
 declare @profileId int, @roleId int, @userId int;      
  set @profileId = (select id from [Profile] where [Name]='Client')      
  set @userId  = (select id from UserList where EmailId=@EmailId and idProfile=@profileId)      
  set @roleId  = (select RoleId from ClientUser where UserId=@userId)      
      
if(@roleId=1)      
      
 Begin      
      
 if(@timeNow is not null)      
  Begin      
   select distinct im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId as candidateEmail, 
   --@EmailId as ClientHrContact,  
   ca.ClientHRAttemptContact as ClientHrContact,
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
   cm.id as CandidateId,ulc.Isonline as IsOnline,ul.Isonline as IsHrOnline, irf.IsQCRequired, ipr.NickName      
   from InterviewMeetings im       
   inner join IRF on im.IRFId = IRF.IRFId       
   left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId       
   left join QCFeedbackReport QCF on im.id=QCF.InterviewId       
   left join UserList ulc on im.candidateEmail= ulc.EmailId      
   join CandidatesMaster cm on ulc.id= cm.UserId      
   join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId      
   inner join TransClientUser tcu on IRF.id=tcu.IRFId      
   left join UserList ul on tcu.ClientUserId= ul.id      
   left join UserList uli on uli.EmailId = im.interviewer
   left join InterviewerProfile ipr on ipr.UserId = uli.id
   where ul.EmailId LIKE @EmailId AND ul.IsActive=1 and  ca.ClientHRAttemptContact=@EmailId --im.ClientHrContact=@EmailId or     
   and irf.IsDeleted=0      
   --and ((irf.IsDeleted = 1 and cast(interviewDate as date) >= cast(DATEADD(day, -30, @timeNow) as date)) or IRF.IsDeleted=0)      
   and cast(interviewDate as date) >= cast(DATEADD(day, -30, @timeNow) as date)      
   and im.confirmationStatus=1 and (ic.InterviewStatus=2 or ic.InterviewStatus=4) and ((irf.IsQCRequired=1 and QCF.QCStatus=1) or irf.IsQCRequired=0 or irf.IsQCRequired is null)      
   order by im.interviewDate desc      
  End      
 else if((@startDate is not null) and (@endDate is not null))      
  Begin      
   select distinct im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId as candidateEmail, 
   --@EmailId as ClientHrContact, 
   ca.ClientHRAttemptContact as ClientHrContact,
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
   cm.id as CandidateId,ulc.Isonline as IsOnline,ul.Isonline as IsHrOnline, irf.IsQCRequired, ipr.NickName      
   from InterviewMeetings im       
   inner join IRF on im.IRFId = IRF.IRFId       
   left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId       
   left join QCFeedbackReport QCF on im.id=QCF.InterviewId       
   left join UserList ulc on im.candidateEmail= ulc.EmailId      
   join CandidatesMaster cm on ulc.id= cm.UserId      
   join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId      
   inner join TransClientUser tcu on IRF.id=tcu.IRFId      
   left join UserList ul on tcu.ClientUserId= ul.id      
   left join UserList uli on uli.EmailId = im.interviewer
   left join InterviewerProfile ipr on ipr.UserId = uli.id   
   where ul.EmailId LIKE @EmailId AND ul.IsActive=1 and ca.ClientHRAttemptContact=@EmailId --im.ClientHrContact=@EmailId or   
   and irf.IsDeleted=0      
   --and ((irf.IsDeleted = 1 and cast(interviewDate as date) >= cast(DATEADD(day, -30, @timeNow) as date)) or IRF.IsDeleted=0)      
   and cast(interviewDate as date) >= cast(@startDate as date) and cast(interviewDate as date) <= cast(@endDate as date)      
   and im.confirmationStatus=1 and (ic.InterviewStatus=2 or ic.InterviewStatus=4) and ((irf.IsQCRequired=1 and QCF.QCStatus=1) or irf.IsQCRequired=0 or irf.IsQCRequired is null)      
   order by im.interviewDate desc      
      
      
  End      
      
 else      
  Begin      
   select distinct im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId as candidateEmail, 
   --@EmailId as ClientHrContact, 
   ca.ClientHRAttemptContact as ClientHrContact,
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
   cm.id as CandidateId,ulc.Isonline as IsOnline,ul.Isonline as IsHrOnline, irf.IsQCRequired, ipr.NickName      
   from InterviewMeetings im       
   inner join IRF on im.IRFId = IRF.IRFId       
   left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId       
   left join QCFeedbackReport QCF on im.id=QCF.InterviewId       
   left join UserList ulc on im.candidateEmail= ulc.EmailId      
   join CandidatesMaster cm on ulc.id= cm.UserId      
   join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId      
   inner join TransClientUser tcu on IRF.id=tcu.IRFId      
   left join UserList ul on tcu.ClientUserId= ul.id      
   left join UserList uli on uli.EmailId = im.interviewer
   left join InterviewerProfile ipr on ipr.UserId = uli.id   
   where ul.EmailId LIKE @EmailId AND ul.IsActive=1 and ca.ClientHRAttemptContact=@EmailId--im.ClientHrContact=@EmailId or    
   and irf.IsDeleted=0      
   --and ((irf.IsDeleted = 1 and cast(interviewDate as date) >= cast(DATEADD(day, -30, @timeNow) as date)) or IRF.IsDeleted=0)      
   and im.confirmationStatus=1 and (ic.InterviewStatus=2 or ic.InterviewStatus=4) and ((irf.IsQCRequired=1 and QCF.QCStatus=1) or irf.IsQCRequired=0 or irf.IsQCRequired is null)      
   order by im.interviewDate desc      
      
  End      
      
      
 End      
      
else if(@roleId=4)      
 Begin      
  if(@timeNow is not null)      
   Begin      
    select im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId as candidateEmail, 
	--ISNULL(ca.ClientHRAttemptContact, im.ClientHrContact) as ClientHrContact,      
	ca.ClientHRAttemptContact as ClientHrContact,
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
    cm.id as CandidateId,ulc.Isonline as IsOnline,ul.Isonline as IsHrOnline, irf.IsQCRequired, ipr.NickName      
    from InterviewMeetings im       
    inner join IRF on im.IRFId = IRF.IRFId       
    left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId       
    left join QCFeedbackReport QCF on im.id=QCF.InterviewId       
    left join UserList ulc on im.candidateEmail= ulc.EmailId      
    join CandidatesMaster cm on ulc.id= cm.UserId      
    join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId      
    inner join TransClientUser tcu on IRF.id=tcu.IRFId      
    left join UserList ul on tcu.ClientUserId= ul.id      
    left join UserList uli on uli.EmailId = im.interviewer
    left join InterviewerProfile ipr on ipr.UserId = uli.id  
    where ul.EmailId LIKE @EmailId AND ul.IsActive=1 and im.staffingAgency=@EmailId      
    and irf.IsDeleted=0      
    --and ((irf.IsDeleted = 1 and cast(interviewDate as date) >= cast(DATEADD(day, -30, @timeNow) as date)) or IRF.IsDeleted=0)      
    and cast(interviewDate as date) >= cast(DATEADD(day, -30, @timeNow) as date)      
    and im.confirmationStatus=1 and (ic.InterviewStatus=2 or ic.InterviewStatus=4) and ((irf.IsQCRequired=1 and QCF.QCStatus=1) or irf.IsQCRequired=0 or irf.IsQCRequired is null)      
    order by im.interviewDate desc      
      
   End      
  else if((@startDate is not null) and (@endDate is not null))      
   Begin      
    select im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId as candidateEmail, 
	--ISNULL(ca.ClientHRAttemptContact, im.ClientHrContact) as ClientHrContact,   
	ca.ClientHRAttemptContact as ClientHrContact,
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
    cm.id as CandidateId,ulc.Isonline as IsOnline,ul.Isonline as IsHrOnline, irf.IsQCRequired, ipr.NickName      
    from InterviewMeetings im       
    inner join IRF on im.IRFId = IRF.IRFId       
    left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId       
    left join QCFeedbackReport QCF on im.id=QCF.InterviewId       
    left join UserList ulc on im.candidateEmail= ulc.EmailId      
    join CandidatesMaster cm on ulc.id= cm.UserId      
    join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId      
    inner join TransClientUser tcu on IRF.id=tcu.IRFId      
    left join UserList ul on tcu.ClientUserId= ul.id      
    left join UserList uli on uli.EmailId = im.interviewer
    left join InterviewerProfile ipr on ipr.UserId = uli.id  
    where ul.EmailId LIKE @EmailId AND ul.IsActive=1 and im.staffingAgency=@EmailId      
    and irf.IsDeleted=0      
    --and ((irf.IsDeleted = 1 and cast(interviewDate as date) >= cast(DATEADD(day, -30, @timeNow) as date)) or IRF.IsDeleted=0)      
    and cast(interviewDate as date) >= cast(@startDate as date) and cast(interviewDate as date) <= cast(@endDate as date)      
    and im.confirmationStatus=1 and (ic.InterviewStatus=2 or ic.InterviewStatus=4) and ((irf.IsQCRequired=1 and QCF.QCStatus=1) or irf.IsQCRequired=0 or irf.IsQCRequired is null)      
    order by im.interviewDate desc      
      
   End      
  else      
   Begin      
    select im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId as candidateEmail, 
	--ISNULL(ca.ClientHRAttemptContact, im.ClientHrContact) as ClientHrContact,   
	ca.ClientHRAttemptContact as ClientHrContact,
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
    cm.id as CandidateId,ulc.Isonline as IsOnline,ul.Isonline as IsHrOnline, irf.IsQCRequired, ipr.NickName      
    from InterviewMeetings im       
    inner join IRF on im.IRFId = IRF.IRFId       
    left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId       
    left join QCFeedbackReport QCF on im.id=QCF.InterviewId       
    left join UserList ulc on im.candidateEmail= ulc.EmailId      
    join CandidatesMaster cm on ulc.id= cm.UserId      
    join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId      
    inner join TransClientUser tcu on IRF.id=tcu.IRFId      
    left join UserList ul on tcu.ClientUserId= ul.id      
    left join UserList uli on uli.EmailId = im.interviewer
    left join InterviewerProfile ipr on ipr.UserId = uli.id   
    where ul.EmailId LIKE @EmailId AND ul.IsActive=1 and im.staffingAgency=@EmailId      
    and irf.IsDeleted=0      
    --and ((irf.IsDeleted = 1 and cast(interviewDate as date) >= cast(DATEADD(day, -30, @timeNow) as date)) or IRF.IsDeleted=0)      
    and im.confirmationStatus=1 and (ic.InterviewStatus=2 or ic.InterviewStatus=4) and ((irf.IsQCRequired=1 and QCF.QCStatus=1) or irf.IsQCRequired=0 or irf.IsQCRequired is null)      
    order by im.interviewDate desc      
      
   End      
      
           
      
 End      
      
else      
 Begin      
  if(@timeNow is not null)      
   Begin      
    select im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId as candidateEmail, 
	--ISNULL(ca.ClientHRAttemptContact, im.ClientHrContact) as ClientHrContact,      
	ca.ClientHRAttemptContact as ClientHrContact,
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
    cm.id as CandidateId,ulc.Isonline as IsOnline,ul.Isonline as IsHrOnline, irf.IsQCRequired, ipr.NickName      
    from InterviewMeetings im       
    inner join IRF on im.IRFId = IRF.IRFId       
    left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId       
    left join QCFeedbackReport QCF on im.id=QCF.InterviewId       
    left join UserList ulc on im.candidateEmail= ulc.EmailId      
    join CandidatesMaster cm on ulc.id= cm.UserId      
    join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId      
    inner join TransClientUser tcu on IRF.id=tcu.IRFId      
    left join UserList ul on tcu.ClientUserId= ul.id      
    left join UserList uli on uli.EmailId = im.interviewer
    left join InterviewerProfile ipr on ipr.UserId = uli.id  
    where ul.EmailId LIKE @EmailId AND ul.IsActive=1      
    and irf.IsDeleted=0      
    --and ((irf.IsDeleted = 1 and cast(interviewDate as date) >= cast(DATEADD(day, -30, @timeNow) as date)) or IRF.IsDeleted=0)      
    and cast(interviewDate as date) >= cast(DATEADD(day, -30, @timeNow) as date)      
    and im.confirmationStatus=1 and (ic.InterviewStatus=2 or ic.InterviewStatus=4) and ((irf.IsQCRequired=1 and QCF.QCStatus=1 ) or irf.IsQCRequired=0 or irf.IsQCRequired is null) 
    order by im.interviewDate desc      
      
      
   End      
  else if((@startDate is not null) and (@endDate is not null))      
   Begin      
    select im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId as candidateEmail, 
	--ISNULL(ca.ClientHRAttemptContact, im.ClientHrContact) as ClientHrContact,    
	ca.ClientHRAttemptContact as ClientHrContact,
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
    cm.id as CandidateId,ulc.Isonline as IsOnline,ul.Isonline as IsHrOnline, irf.IsQCRequired, ipr.NickName     
    from InterviewMeetings im       
    inner join IRF on im.IRFId = IRF.IRFId       
    left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId       
    left join QCFeedbackReport QCF on im.id=QCF.InterviewId       
    left join UserList ulc on im.candidateEmail= ulc.EmailId      
    join CandidatesMaster cm on ulc.id= cm.UserId      
    join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId      
    inner join TransClientUser tcu on IRF.id=tcu.IRFId      
    left join UserList ul on tcu.ClientUserId= ul.id      
    left join UserList uli on uli.EmailId = im.interviewer
    left join InterviewerProfile ipr on ipr.UserId = uli.id   
    where ul.EmailId LIKE @EmailId AND ul.IsActive=1      
    and irf.IsDeleted=0      
    --and ((irf.IsDeleted = 1 and cast(interviewDate as date) >= cast(DATEADD(day, -30, @timeNow) as date)) or IRF.IsDeleted=0)      
    and cast(interviewDate as date) >= cast(@startDate as date) and cast(interviewDate as date) <= cast(@endDate as date)      
    and im.confirmationStatus=1 and (ic.InterviewStatus=2 or ic.InterviewStatus=4) and ((irf.IsQCRequired=1 and QCF.QCStatus=1) or irf.IsQCRequired=0 or irf.IsQCRequired is null)      
    order by im.interviewDate desc      
      
   End      
  else      
   Begin      
    select im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId as candidateEmail, 
	--ISNULL(ca.ClientHRAttemptContact, im.ClientHrContact) as ClientHrContact, 
	ca.ClientHRAttemptContact as ClientHrContact,
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
    cm.id as CandidateId,ulc.Isonline as IsOnline,ul.Isonline as IsHrOnline, irf.IsQCRequired, ipr.NickName      
    from InterviewMeetings im       
    inner join IRF on im.IRFId = IRF.IRFId       
    left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId       
    left join QCFeedbackReport QCF on im.id=QCF.InterviewId       
    left join UserList ulc on im.candidateEmail= ulc.EmailId      
    join CandidatesMaster cm on ulc.id= cm.UserId      
    join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId      
    inner join TransClientUser tcu on IRF.id=tcu.IRFId      
    left join UserList ul on tcu.ClientUserId= ul.id      
    left join UserList uli on uli.EmailId = im.interviewer
    left join InterviewerProfile ipr on ipr.UserId = uli.id   
    where ul.EmailId LIKE @EmailId AND ul.IsActive=1      
    and irf.IsDeleted=0      
    --and ((irf.IsDeleted = 1 and cast(interviewDate as date) >= cast(DATEADD(day, -30, @timeNow) as date)) or IRF.IsDeleted=0)      
    and im.confirmationStatus=1 and (ic.InterviewStatus=2 or ic.InterviewStatus=4) and ((irf.IsQCRequired=1 and QCF.QCStatus=1) or irf.IsQCRequired=0 or irf.IsQCRequired is null)      
    order by im.interviewDate desc      
          
   End      
        
           
      
 End      
            
END  
GO


