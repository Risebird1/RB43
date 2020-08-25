/****** Object:  StoredProcedure [dbo].[usp_AddRecruiter]    Script Date: 08-01-2020 6.44.13 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRiseTubeMeetingsForCuratedUser'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRiseTubeMeetingsForCuratedUser
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetRiseTubeMeetingsForCuratedUser]        
     @clientid int  ,    
  @Emailid varchar(100)    
AS        
Begin       
DECLARE @id int      
 Select @id=id from UserList where EmailId=@Emailid      
if @clientid = 0      
 BEGIN        
       
select distinct im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,  
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,  
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,  
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,   
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,  
IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,  
ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage     
as ThumbnailImage,cl.CuratedLink as CuratedLink, QCF.InterviewId as QCInterview,(Select top 1 status from SuitableCandidate where InterviewId= ic.InterviewId  order by InterstedOn desc)  as IsIntrested ,(Select top 1 status from RiseTubeVedioShortList where InterviewId= ic.InterviewId  order by ShortListedOn desc)  as ShortListstatus,ic.CandidateAvailable    
from InterviewMeetings im          
inner join IRF on im.IRFId = IRF.IRFId        
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId     
left join UserList ul on ul.EmailId = im.candidateEmail  
join CandidatesMaster cm on cm.UserId = ul.id      
join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id  
join CandidateCuratedScore ccs on ul.EmailId= ccs.CandidateEmailId  
left join QCFeedbackReport QCF on im.id=QCF.InterviewId      
inner join client cl on IRF.ClientID=cl.id      
inner join CuratedIRFMapping cim on cim.IRFId = irf.id  
inner join CuratedGroupSubscribers cgs on cgs.CuratedGroupId = cim.CuratedGroupId  
where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and cgs.UserId=@id  and ccs.CuratedEmailId=@Emailid and ccs.Score>=30  
END      
else      
 BEGIN        
select distinct im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,  
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,  
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,  
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,   
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,  
IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,  
ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage     
as ThumbnailImage,QCF.InterviewId as QCInterview,  
(Select top 1 status from SuitableCandidate where InterviewId= ic.InterviewId order by InterstedOn desc) as IsIntrested ,  
(Select top 1 status from RiseTubeVedioShortList where InterviewId= ic.InterviewId order by ShortListedOn desc) as ShortListstatus,ic.CandidateAvailable   
from InterviewMeetings im          
inner join IRF on im.IRFId = IRF.IRFId        
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId     
left join UserList ul on ul.EmailId = im.candidateEmail  
join CandidatesMaster cm on ul.id = cm.UserId    
join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId =irf.id  
inner join QCFeedbackReport QCF on im.id=QCF.InterviewId      
inner join CuratedIRFMapping cim on cim.IRFId = irf.id  
inner join CuratedGroupSubscribers cgs on cgs.CuratedGroupId = cim.CuratedGroupId  
where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and IRF.Clientid=@clientid and cgs.UserId=@id      
END      
         
End         
  
  

Go