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
select IRF.IRFId as IRFName, IRF.JDLink, ul.EmailId, cm.id as CandidateId, ca.IRFId, ca.EmailId as CandidateEmailId, ca.[Name] as CandidateName, ca.ResumeLink, ul2.EmailId as RisebirdHR, ca.JoinabilityScore, ca.SelectabilityScore, ca.Phone, ca.AddedByUserId, ca.TechSkillScore, ca.SkillGeneralScore, ca.VoiceItUserId 
from Candidate ca join UserList ul on ul.id = ca.AddedByUserId
join CandidatesMaster cm on ca.EmailId = cm.EmailId
join IRF on ca.IRFId =IRF.id
join UserList ul2 on irf.RisebirdHr=ul2.id
where ca.AddedByUserId = @userId
END
GO


