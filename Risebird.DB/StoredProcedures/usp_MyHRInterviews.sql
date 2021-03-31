/****** Object:  StoredProcedure [dbo].[usp_MyHRInterviews]    Script Date: 03-07-2020 9.21.21 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_MyHRInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_MyHRInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

 
CREATE PROCEDURE [dbo].[usp_MyHRInterviews]     
@emailID varchar(200)        
AS      
BEGIN     
declare @hrId int
set @hrId = (select HR.id from UserList ul join HR on HR.UserId=ul.id where EmailId=@EmailId and idProfile=2)

select im.id, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact, ulc.MobileNumber as ClientHrMobile,im.staffingAgency, uls.MobileNumber as StaffingAgencyMobile,
im.confirmationStatus, im.interviewer, im.candidateName, im.QCInterviewer, ca.Phone as MobileNumber, im.zoomLink,
ca.ResumeLink as resumeLink ,irf.JDLink, ca.CandidateMasterId, uli.MobileNumber as InterviewerPhone, im.HRComment, IRF.JDTiltle, IRF.id as irfIntId,IRF.ClientName,
im.HRImageFileURL, im.candidateEmail, ul.FirstName as candidateName, 
ca.ClientHRAttemptContact as CandidateClientHR, irf.ClientName, cl.[Name] as ClientCompName
from InterviewMeetings im   
inner join IRF  irf on im.IRFId=irf.IRFId
left join UserList ul on ul.EmailId = im.candidateEmail
join Client cl on cl.id = irf.ClientId
join  CandidatesMaster cm on ul.id=cm.UserId
join Candidate ca on irf.id=ca.IRFId and cm.id = ca.CandidateMasterId
left join UserList uli on uli.EmailId = im.interviewer
left join UserList ulc on ulc.EmailId = ca.ClientHRAttemptContact
left join UserList uls on uls.EmailId = im.staffingAgency
join TransHR th on th.IRFId = irf.id
where HRCoordinatorEmailId =@emailID and confirmationStatus = 1 and th.HRId=@hrId

End
GO


