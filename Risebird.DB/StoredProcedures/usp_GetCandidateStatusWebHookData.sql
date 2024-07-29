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

select * from CandidateStatusWebHookData
  

End  
  
  
GO


