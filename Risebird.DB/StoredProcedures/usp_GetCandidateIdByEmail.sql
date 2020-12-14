/****** Object:  StoredProcedure [dbo].[usp_GetCandidateIdByEmail]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateIdByEmail'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateIdByEmail
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
  
CREATE PROCEDURE [dbo].[usp_GetCandidateIdByEmail]  
  
@emailId varchar(100)  
  
AS  
Begin  

declare  

@UserId int = (select id from UserList where EmailId=@emailId and idProfile=23)  

select id from CandidatesMaster where UserId =@UserId
  

End  
  
  
GO


