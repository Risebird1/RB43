IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.sp_RisebirdClientUser'))
BEGIN
    DROP PROCEDURE dbo.sp_RisebirdClientUser
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_RisebirdClientUser] 
  
AS
BEGIN
select cu.id, ul.FirstName,ul.LastName,ul.EmailId,Ul.MobileNumber,ul.idProfile,cl.Name as ClientName,cr.Name as Role from [ClientUser] cu
inner join [dbo].[UserList] ul on cu.UserId = ul.id
inner join [dbo].[Client] cl on cu.ClientId= cl.id
inner join [ClientRole] cr on cu.RoleId= cr.id
where ul.IsActive =1

END
GO 




