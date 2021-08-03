  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetUserById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUserById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
CREATE PROCEDURE [dbo].[usp_GetUserById] (@id int)  
AS  
Begin  
 Declare @uid int  
 SET @uid = @id  
 SELECT [idProfile], [FirstName], [LastName], [EmailId], [MobileNumber],[WhatsappNumber] ,[IsActive] 
   FROM [dbo].[UserList] where Id = @uid  
End  
  
  GO