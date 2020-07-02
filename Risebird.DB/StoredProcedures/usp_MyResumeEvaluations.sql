/****** Object:  StoredProcedure [dbo].[usp_OpenResumeEvaluations]    Script Date: 11-12-2019 2.24.56 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_MyResumeEvaluations'))
BEGIN
    DROP PROCEDURE dbo.usp_MyResumeEvaluations
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_MyResumeEvaluations] 
   @emailID varchar(200)     
AS
BEGIN
select IRF.IRFId as IRFName, IRF.JDLink,IRF.IsBiometricEnabled,tre.*,ul.EmailId, tim.id,tim.ResumeEvaluatorEmailId,
tim.CandidateId,ulc.EmailId as CandidateEmailId, CASE WHEN ulc.LastName is not null THEN ulc.FirstName + ulc.LastName ELSE ul.FirstName END as CandidateName, ca.ResumeLink,
ul2.EmailId as RisebirdHR, ca.JoinabilityScore, ca.SelectabilityScore ,ulc.MobileNumber as Phone, ca.VoiceItUserId 
from TransResumeEvaluator tre join ResumeEvaluator re on re.id=tre.ResumeEvaluatorId 
join UserList ul on ul.id = re.UserId join TransHREvaluatorIRF tim on tim.IRFId = tre.IRFId 
join CandidatesMaster cm on tim.CandidateId = cm.id 
join Candidate ca on tre.IRFId = ca.IRFId and ca.CandidateMasterId = cm.id 
join IRF on tre.IRFId =IRF.id
join UserList ul2 on irf.RisebirdHr=ul2.id
join UserList ulc on ulc.id = cm.UserId
where ul.EmailId=@emailID and tim.ResumeEvaluatorEmailId = @emailID
END
GO


