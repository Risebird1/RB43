/****** Object:  StoredProcedure [dbo].[usp_GetClientUserById]    Script Date: 5/5/2021 11:22:35 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientUserById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientUserById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetClientUserById] (@id int)  
AS  
Begin  
 Declare @cid int  
 SET @cid = @id  
   SELECT [FirstName], [LastName], [EmailId], [MobileNumber] , cr.Name as Role ,   
   cl.Name as ClientName, cl.id as SelectedClient, cr.id as SelectedClientRole, UserPassword,ul.WhatsappNumber,ul.IsActive  
   FROM [dbo].[UserList] ul JOIN [dbo].[ClientUser] cu on ul.id = cu.UserId  
   JOIN [ClientRole] cr on cu.RoleId= cr.id  
   JOIN [dbo].[Client] cl on cu.ClientId= cl.id WHERE cu.id = @cid  
End  

GO


