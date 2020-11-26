/****** Object:  StoredProcedure [dbo].[usp_GetCandidateStatusList]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateStatusList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateStatusList
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetCandidateStatusList]  
@IRFId int,   
@CandidateId int
AS  
Begin

	select cs.*, csom.StatusValue from CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status]=csom.id where IRFId=@IRFId and CandidateId = @CandidateId

End
GO


