IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('sp_GetUserProfileBasedOnEmailId'))
BEGIN
    DROP PROCEDURE sp_GetUserProfileBasedOnEmailId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_GetUserProfileBasedOnEmailId]   
  @EmailId nvarchar(300)
AS  
BEGIN  
select distinct pl.Name  from	[dbo].[Profile] pl      
			join [dbo].[UserList] ul on pl.id = ul.idprofile  
			where ul.EmailId LIKE @EmailId

End   
  

GO







