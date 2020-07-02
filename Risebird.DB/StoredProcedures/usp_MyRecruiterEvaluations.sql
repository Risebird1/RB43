/****** Object:  StoredProcedure [dbo].[usp_MyRecruiterEvaluations]    Script Date: 09-01-2020 3.34.08 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_MyRecruiterEvaluations'))
BEGIN
    DROP PROCEDURE dbo.usp_MyRecruiterEvaluations
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_MyRecruiterEvaluations] 
   @userId int     
AS
BEGIN
select IRF.IRFId as IRFName, IRF.JDLink,IRF.IsBiometricEnabled, ul.EmailId,
cm.id as CandidateId, ca.IRFId, ulc.EmailId as CandidateEmailId,
CASE WHEN ulc.LastName is not null THEN ulc.FirstName + ulc.LastName ELSE ulc.FirstName END as CandidateName,
ca.ResumeLink, ul2.EmailId as RisebirdHR, ca.JoinabilityScore, ca.SelectabilityScore,ca.Phone as Phone,
ca.AddedByUserId, ca.TechSkillScore, ca.SkillGeneralScore, ca.VoiceItUserId 
from Candidate ca
join UserList ul on ul.id = ca.AddedByUserId
join CandidatesMaster cm on ca.CandidateMasterId = cm.id
join IRF on ca.IRFId =IRF.id
join UserList ul2 on irf.RisebirdHr=ul2.id
join UserList ulc on cm.UserId = ulc.id
where ca.AddedByUserId = @userId
END
GO


