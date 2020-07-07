/****** Object:  StoredProcedure [dbo].[usp_OpenResumeEvaluations]    Script Date: 10-12-2019 7.06.54 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_OpenResumeEvaluations'))
BEGIN
    DROP PROCEDURE dbo.usp_OpenResumeEvaluations
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE PROCEDURE [dbo].[usp_OpenResumeEvaluations] 
   @emailID varchar(200)     
AS
BEGIN
select IRF.IRFId as IRFName, IRF.JDLink,tre.*,ul.EmailId, tim.id,tim.ResumeEvaluatorEmailId, tim.CandidateId,
ulc.EmailId as CandidateEmailId, 
CASE WHEN ul.LastName is not null THEN ul.FirstName + ' ' + ul.LastName ELSE ul.FirstName END as CandidateName,
ca.ResumeLink, ul2.EmailId as RisebirdHR  
from TransResumeEvaluator tre join ResumeEvaluator re on re.id=tre.ResumeEvaluatorId 
join UserList ul on ul.id = re.UserId join TransHREvaluatorIRF tim on tim.IRFId = tre.IRFId 
join CandidatesMaster cm on tim.CandidateId = cm.id 
join Candidate ca on tre.IRFId = ca.IRFId and ca.CandidateMasterId = cm.id 
join IRF on tre.IRFId =IRF.id
join UserList ul2 on irf.RisebirdHr=ul2.id
join UserList ulc on ulc.id = cm.UserId
where ul.EmailId=@emailID and tim.ResumeEvaluatorEmailId is NULL

END
GO


