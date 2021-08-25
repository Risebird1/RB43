/****** Object:  StoredProcedure [dbo].[usp_GetCandidateStaffingAgencyAndClientHRByInterviewId]    ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateStaffingAgencyAndClientHRByInterviewId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateStaffingAgencyAndClientHRByInterviewId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_GetCandidateStaffingAgencyAndClientHRByInterviewId]
@interviewId int
AS
Begin

select a.EmailId from (select distinct ca.ProfileSource as EmailId from InterviewMeetings im 
	join IRF on irf.IRFId =im.IRFId
	join UserList ul on ul.EmailId = im.candidateEmail
	join CandidatesMaster cm on cm.UserId = ul.id
	join Candidate ca on ca.CandidateMasterId =cm.id and ca.IRFId =irf.id
	where im.id =@interviewId
	union
	select distinct ca.ClientHRAttemptContact as EmailId from InterviewMeetings im 
	join IRF on irf.IRFId =im.IRFId
	join UserList ul on ul.EmailId = im.candidateEmail
	join CandidatesMaster cm on cm.UserId = ul.id
	join Candidate ca on ca.CandidateMasterId =cm.id and ca.IRFId =irf.id
	where im.id =@interviewId) a
	where EmailId is not null

End

GO

