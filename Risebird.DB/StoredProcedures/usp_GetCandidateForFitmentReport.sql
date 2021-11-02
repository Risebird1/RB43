/****** Object:  StoredProcedure [dbo].[usp_GetCandidateForFitmentReport]   ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateForFitmentReport'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateForFitmentReport
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_GetCandidateForFitmentReport]

@candidateMasterId int,
@irfId int

AS
Begin  
  
select distinct cm.id, CASE WHEN ul.LastName is not null THEN ul.FirstName + ' ' + ul.LastName ELSE ul.FirstName END as [Name],
ca.ResumeLink, ul.EmailId, ca.CurrentEmployer, ca.[Location], IRF.IRFId as IRFName,
ca.FitmentStatus, ca.PriorityStatus, ca.CompletionStatus, ca.JoinabilityComments, ca.SelectabilityComments
from CandidatesMaster cm join UserList ul on ul.id = cm.UserId 
join Candidate ca on ca.CandidateMasterId = cm.id join IRF on IRF.id = ca.IRFId
where cm.id = @candidateMasterId and ca.IRFId = @irfId 
    
End

GO


