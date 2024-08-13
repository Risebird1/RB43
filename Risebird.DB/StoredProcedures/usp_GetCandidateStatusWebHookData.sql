/****** Object:  StoredProcedure [dbo].[usp_GetCandidateStatusWebHookData]    ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateStatusWebHookData'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateStatusWebHookData
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
CREATE PROCEDURE [dbo].[usp_GetCandidateStatusWebHookData]  
  
@timeNow datetime
AS  
Begin  

select cswh.*, Project.ProjectUniqueid as jobId from CandidateStatusWebHookData cswh
join IRF on irf.IRFId = cswh.IRFId
outer apply (Select top 1 ProjectUniqueid From ProjectIRFMapping pim join Project pr on pr.id=pim.ProjectId where pim.IRFId=IRF.id) Project 
  
End  
  
  
GO


