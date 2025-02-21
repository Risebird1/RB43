/****** Object:  StoredProcedure [dbo].[usp_GetCandidateUniqueId]  ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetUniqueIdByEmailId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUniqueIdByEmailId
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE[dbo].[usp_GetUniqueIdByEmailId]
@emailId varchar(200)
AS
Begin        
      
select ul.uniqueid from UserList ul   
where ul.EmailId = @emailId
      
End 