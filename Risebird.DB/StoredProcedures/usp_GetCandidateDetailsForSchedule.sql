IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCandidateDetailsForSchedule'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateDetailsForSchedule
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetCandidateDetailsForSchedule
@irfid int,
@CandidateEmail varchar(200)
AS
BEGIN
Select ca.ResumeLink,ca.Phone,IRF.IRFId,cl.tenantId,ca.ClientHRAttemptContact,LTRIM(RTRIM(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,''))) as CandidateName,ISNULL(sc.duration,60) as duration 
From Candidate ca join CandidatesMaster cm on cm.id=ca.CandidateMasterId join UserList ul on ul.id=cm.UserId
join IRF on IRF.id=ca.IRFId
join Client cl on cl.id=IRF.ClientId
left join ScheduleInterviewEvent sc on sc.SkillChannel=IRF.IRFId
where ca.IRFId=@irfid and ul.EmailId=@CandidateEmail
END
GO