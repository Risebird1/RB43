/****** Object:  StoredProcedure [dbo].[usp_GetClientRoleAndCompanyNameByEmailId]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientRoleAndCompanyNameByEmailId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientRoleAndCompanyNameByEmailId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
  
CREATE PROCEDURE [dbo].[usp_GetClientRoleAndCompanyNameByEmailId]  
  
@emailId varchar(100)
  
AS  
Begin  

select cu.ClientId, cl.[Name],cl.LogoLink from ClientUser cu join UserList ul on ul.id = cu.UserId
join Client cl on cl.id = cu.ClientId
where ul.EmailId = @emailId 

End  
  
  
GO


