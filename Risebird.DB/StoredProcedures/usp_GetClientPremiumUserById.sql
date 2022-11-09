/****** Object:  StoredProcedure [dbo].[usp_GetClientUserById]    Script Date: 5/5/2021 11:22:35 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientPremiumUserById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientPremiumUserById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetClientPremiumUserById] (@id int)  
AS  
Begin  
 Declare @cid int  
 SET @cid = @id  
   SELECT [FirstName], [LastName], [EmailId], [MobileNumber] ,   
   cl.Name as ClientName, cl.id as SelectedClient, UserPassword,ul.WhatsappNumber,cu.MCQValidation  
   FROM [dbo].[UserList] ul JOIN [dbo].[ClientPremiumUser] cu on ul.id = cu.UserId  
   JOIN [dbo].[Client] cl on cu.ClientId= cl.id WHERE cu.id = @cid  
End  
GO


