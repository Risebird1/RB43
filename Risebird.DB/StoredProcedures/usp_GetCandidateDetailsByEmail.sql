IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateDetailsByEmail'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateDetailsByEmail
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetCandidateDetailsByEmail
@IRFId varchar(15),
@EmailId varchar(200)
AS
BEGIN
Select LTrim(Rtrim(IsNull(ul.FirstName,'')+' '+Isnull(ul.LastName,''))) as CandidateName,ul.EmailId as candidateEmail,
ca.ResumeLink,ca.ClientHRAttemptContact as ClientHrContact,ul.MobileNumber  
From Candidate ca join CandidatesMaster cm on cm.id=ca.CandidateMasterId join UserList ul on ul.Id=cm.UserId
join IRF on ca.IRFId=IRF.id
where IRF.IRFId=@IRFId and ul.EmailId=@EmailId
END
GO



