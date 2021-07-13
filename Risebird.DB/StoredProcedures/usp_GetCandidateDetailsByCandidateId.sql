

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateDetailsByCandidateId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateDetailsByCandidateId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetCandidateDetailsByCandidateId]  
@IRFId int,   
@CandidateId int
AS  
Begin
	select distinct irf.IRFId as IRFName,
	case 
		when ul.LastName is null then ul.FirstName
		else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))
    end as [Name]
	from Candidate ca join CandidatesMaster cm on ca.CandidateMasterId = cm.id
	join UserList ul on ul.id = cm.UserId
	join IRF on irf.id = ca.IRFId
	where ca.IRFId = @IRFId and ca.CandidateMasterId = @CandidateId


End
GO


