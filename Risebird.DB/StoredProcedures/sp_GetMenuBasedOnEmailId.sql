IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.sp_GetMenuBasedOnEmailId'))
BEGIN
    DROP PROCEDURE dbo.sp_GetMenuBasedOnEmailId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_GetMenuBasedOnEmailId]   
  @EmailId nvarchar(300)     
AS  
BEGIN    
DECLARE @lastLogin AS varchar(200);    
select @lastLogin=LastLogin from [dbo].[UserList] where EmailId LIKE @EmailId        
update [dbo].[UserList]      
set LastLogin= getdate(),isonline=1      
where EmailId LIKE @EmailId     
if(@EmailId!='ExternalUser')  
begin  
select fn.functionname,fn.description,fn.idparentfunction,fn.sicon,fn.surl,fn.idisplayorder,pl.Name profileName,    
pf.dAccess,@lastLogin as NavigationShow,c.LogoLink from [dbo].[Function] fn      
join [dbo].[ProfileFunction] pf on fn.id = pf.idfunction      
join [dbo].[Profile] pl on pl.id = pf.idprofile      
join [dbo].[UserList] ul on pl.id = ul.idprofile 
left join ClientUser cu on cu.UserId=ul.id
left join Client c on c.id=cu.ClientId
where ul.EmailId LIKE @EmailId AND ul.IsActive=1 order by idisplayorder      
  end  
  else  
  begin  
  select distinct fn.functionname,fn.description,fn.idparentfunction,fn.sicon,fn.surl,fn.idisplayorder,pl.Name profileName,    
pf.dAccess,@lastLogin as NavigationShow from [dbo].[Function] fn      
join [dbo].[ProfileFunction] pf on fn.id = pf.idfunction      
join [dbo].[Profile] pl on pl.id = pf.idprofile      
join [dbo].[UserList] ul on pl.id = ul.idprofile    
where pl.Name='ExternalUser'  
END      
End  
  

GO 




