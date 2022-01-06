/****** Object:  StoredProcedure [dbo].[usp_GetClientRoleByEmailId]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientRoleByEmailId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientRoleByEmailId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
  
CREATE PROCEDURE [dbo].[usp_GetClientRoleByEmailId]  
  
@emailId varchar(100)  
  
AS  
Begin  

select cu.RoleId from ClientUser cu join UserList ul on ul.id = cu.UserId
where ul.EmailId = @emailId 

End  
  
  
GO


