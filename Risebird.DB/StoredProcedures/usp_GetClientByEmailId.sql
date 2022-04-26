/****** Object:  StoredProcedure [dbo].[usp_GetClientByEmailId]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientByEmailId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientByEmailId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
  
CREATE PROCEDURE [dbo].[usp_GetClientByEmailId]  
  
@emailId varchar(100)  
  
AS  
Begin  

select cu.ClientId from ClientUser cu join UserList ul on ul.id = cu.UserId
where ul.EmailId = @emailId 

End  
  
  
GO


