/****** Object:  StoredProcedure [dbo].[usp_GetCandidateStatusById]    Script Date: 25-11-2020 3.14.02 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateStatusById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateStatusById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
CREATE PROCEDURE [dbo].[usp_GetCandidateStatusById]  
  
@id int  
  
AS  
Begin  

select * from CandidateStatus where id=@id
  

End  
  
  
GO


