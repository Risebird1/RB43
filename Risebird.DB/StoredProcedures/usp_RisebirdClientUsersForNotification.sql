IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_RisebirdClientUsersForNotification'))
BEGIN
    DROP PROCEDURE dbo.usp_RisebirdClientUsersForNotification
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_RisebirdClientUsersForNotification]     
AS    
BEGIN    
select cu.id, ul.FirstName,ul.LastName,ul.EmailId,ul.UserPassword,Ul.MobileNumber,ul.idProfile,cl.Name as ClientName,cr.Name as Role, ul.id as UserId,ul.uniqueid from [ClientUser] cu    
inner join [dbo].[UserList] ul on cu.UserId = ul.id    
inner join [dbo].[Client] cl on cu.ClientId= cl.id    
inner join [ClientRole] cr on cu.RoleId= cr.id    
where ul.IsActive =1 and cu.RoleId!=9   
END
GO