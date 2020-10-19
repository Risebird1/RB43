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
select im.id, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact, ulc.MobileNumber as ClientHrMobile,im.staffingAgency, uls.MobileNumber as StaffingAgencyMobile,
im.confirmationStatus, im.interviewer, im.candidateName, im.QCInterviewer, ca.Phone as MobileNumber,
ca.ResumeLink as resumeLink ,irf.JDLink, ca.CandidateMasterId, uli.MobileNumber as InterviewerPhone, im.HRComment, IRF.JDTiltle, IRF.id as irfIntId from InterviewMeetings im   
inner join IRF  irf on im.IRFId=irf.IRFId
left join UserList ul on ul.EmailId = im.candidateEmail
join  CandidatesMaster cm on ul.id=cm.UserId
join Candidate ca on irf.id=ca.IRFId and cm.id = ca.CandidateMasterId
left join UserList uli on uli.EmailId = im.interviewer
left join UserList ulc on ulc.EmailId = im.ClientHrContact
left join UserList uls on uls.EmailId = im.staffingAgency
where HRCoordinatorEmailId =@emailID and confirmationStatus = 1          
      
END
GO


