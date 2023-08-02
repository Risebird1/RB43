IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.sp_RisebirdClientUser'))
BEGIN
    DROP PROCEDURE dbo.sp_RisebirdClientUser
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_RisebirdClientUser]   
@EmailId varchar(150)    
AS  
BEGIN  
DECLARE @Role varchar(100)
Select @Role=Name from Profile Where id in(Select idProfile From UserList where Emailid=@EmailId)
if(@Role='SeniorHR' or @Role='SupportAdmin')
BEGIN
select cu.id, ul.FirstName,ul.LastName,ul.EmailId,ul.UserPassword,Ul.MobileNumber,ul.idProfile,cl.Name as ClientName,cr.Name as Role, ul.id as UserId,ul.IsActive, cl.logolink from [ClientUser] cu  
inner join [dbo].[UserList] ul on cu.UserId = ul.id  
inner join [dbo].[Client] cl on cu.ClientId= cl.id  
inner join [ClientRole] cr on cu.RoleId= cr.id  
join SeniorHR sh on sh.Clientid=cl.id
left join Userlist ulsh on ulsh.id=sh.Userid
--where ul.IsActive =1 and ulsh.EmailId=@EmailId
where ulsh.EmailId=@EmailId
END
ELSE
BEGIN
select cu.id, ul.FirstName,ul.LastName,ul.EmailId,ul.UserPassword,Ul.MobileNumber,ul.idProfile,cl.Name as ClientName,cr.Name as Role, ul.id as UserId,ul.IsActive,cl.logolink from [ClientUser] cu  
inner join [dbo].[UserList] ul on cu.UserId = ul.id  
inner join [dbo].[Client] cl on cu.ClientId= cl.id  
inner join [ClientRole] cr on cu.RoleId= cr.id  
--where ul.IsActive =1  
END  
END  
GO 




