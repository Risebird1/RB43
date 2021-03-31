/****** Object:  StoredProcedure [dbo].[usp_HROpenInterviews]    Script Date: 03-07-2020 9.21.39 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_HROpenInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_HROpenInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_HROpenInterviews] 
@EmailId varchar(100)
AS
BEGIN
declare @hrId int
set @hrId = (select HR.id from UserList ul join HR on HR.UserId=ul.id where EmailId=@EmailId and idProfile=2)

select im.id, im.IRFId,irf.Id as irfIntId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,
im.confirmationStatus, im.interviewer, im.candidateName, im.QCInterviewer,
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,irf.ClientName, cl.[Name] as ClientCompName,
ca.ClientHRAttemptContact as CandidateClientHR, irf.JDTiltle,
irf.JDLink from InterviewMeetings im  
inner join IRF irf on im.IRFId=irf.IRFId   
left join [dbo].[UserList] ul on im.candidateEmail = ul.EmailId
join CandidatesMaster cm on cm.UserId = ul.id
join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId=irf.id
join Client cl on cl.id = irf.ClientId
join TransHR th on th.IRFId=irf.id
where HRCoordinatorEmailId is null and confirmationStatus = 1 and th.HRId=@hrId
END
GO


