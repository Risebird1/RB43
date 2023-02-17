/****** Object:  StoredProcedure [dbo].[usp_GetCandidateUniqueId]  ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCandidateUniqueId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateUniqueId
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE[dbo].[usp_GetCandidateUniqueId]
@emailId varchar(100)
AS
Begin        
      
select ul.uniqueid from UserList ul   
where ul.EmailId = @emailId  and  ul.idProfile = 23 
      
End 