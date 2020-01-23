/****** Object:  StoredProcedure [dbo].[usp_MyResumeEvaluations]    Script Date: 12-12-2019 7.35.42 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_MyHREvaluations'))
BEGIN
    DROP PROCEDURE dbo.usp_MyHREvaluations
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

create PROCEDURE [dbo].[usp_MyHREvaluations] 
   @emailID varchar(200)     
AS
BEGIN
select IRF.IRFId as IRFName, IRF.JDLink,thr.*,ul.EmailId, tim.id,tim.HREmailId, tim.CandidateId,cm.EmailId as CandidateEmailId, ca.[Name] as CandidateName, ca.ResumeLink, ul2.EmailId as RisebirdHR, ca.JoinabilityScore, ca.SelectabilityScore, ca.Phone, ca.VoiceItUserId   
from TransHR thr join HR  on HR.id=thr.HRId 
join UserList ul on ul.id = HR.UserId join TransHREvaluatorIRF tim on tim.IRFId = thr.IRFId 
join CandidatesMaster cm on tim.CandidateId = cm.id 
join Candidate ca on thr.IRFId = ca.IRFId and ca.EmailId = cm.EmailId 
join IRF 
join UserList ul2 on irf.RisebirdHr=ul2.id
on thr.IRFId =IRF.id
where ul.EmailId=@emailID and tim.HREmailId = @emailID
END
GO


