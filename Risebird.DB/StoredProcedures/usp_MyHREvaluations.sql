/****** Object:  StoredProcedure [dbo].[usp_MyResumeEvaluations]    Script Date: 12-12-2019 7.35.42 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_MyHREvaluations'))
BEGIN
    DROP PROCEDURE dbo.usp_MyHREvaluations
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_MyHREvaluations] 
   @emailID varchar(200)     
AS
BEGIN
select IRF.IRFId as IRFName, IRF.JDLink,thr.*,IRF.IsBiometricEnabled,ul.EmailId, tim.id,tim.HREmailId, tim.CandidateId,ulc.EmailId as CandidateEmailId, CASE WHEN ulc.LastName is not null THEN ulc.FirstName + ulc.LastName ELSE ulc.FirstName END as CandidateName, ca.ResumeLink, ul2.EmailId as RisebirdHR, ca.JoinabilityScore, ca.SelectabilityScore, ca.Phone as Phone, ca.VoiceItUserId    
from TransHR thr join HR  on HR.id=thr.HRId 
join UserList ul on ul.id = HR.UserId join TransHREvaluatorIRF tim on tim.IRFId = thr.IRFId 
join CandidatesMaster cm on tim.CandidateId = cm.id 
join Candidate ca on thr.IRFId = ca.IRFId and ca.CandidateMasterId = cm.id 
join IRF on thr.IRFId =IRF.id
join UserList ul2 on irf.RisebirdHr=ul2.id
join UserList ulc on ulc.id = cm.UserId
where ul.EmailId=@emailID and tim.HREmailId = @emailID
END
GO



