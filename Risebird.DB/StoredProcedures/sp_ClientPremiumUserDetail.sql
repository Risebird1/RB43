IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.sp_ClientPremiumUserDetail'))
BEGIN
    DROP PROCEDURE dbo.sp_ClientPremiumUserDetail
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[sp_ClientPremiumUserDetail]     
  @Email varchar(200)    
AS    
BEGIN    
select cu.id, ul.FirstName,ul.LastName,ul.EmailId,Ul.MobileNumber,ul.idProfile,cl.Name as ClientName,cl.id as ClientId,ul.id as UserId,cl.IsQCRequired,cl.IsRedoInterview
from [ClientPremiumUser] cu    
inner join [dbo].[UserList] ul on cu.UserId = ul.id    
inner join [dbo].[Client] cl on cu.ClientId= cl.id    
where ul.IsActive =1 and ul.emailid =@Email    
    
END
Go