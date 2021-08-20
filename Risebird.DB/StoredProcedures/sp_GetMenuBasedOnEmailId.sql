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

	DECLARE @lastLogin AS varchar(200), @profile varchar(50), @roleId int;    
	select @lastLogin=LastLogin from [dbo].[UserList] where EmailId LIKE @EmailId  
	set @roleId =0
	
	set @profile = (select pr.[Name] from UserList ul join [Profile] pr on pr.id=ul.idProfile where EmailId like @EmailId and ul.IsActive=1)
	

	if(@profile = 'Client')
	begin
		select @roleId=cu.RoleId from UserList ul join ClientUser cu on cu.UserId =ul.id where ul.EmailId=@EmailId
	end

	update [dbo].[UserList] set LastLogin= getdate(),isonline=1 where EmailId LIKE @EmailId    
	
		
	if(@EmailId!='ExternalUser')  
		begin
			if(@profile='Interviewer')

			Begin
			
				declare @userId int;
				set @userId = (select id from UserList where EmailId like @EmailId and IsActive=1)
				
				IF Exists(select 1 from InterviewerProfile where UserId=@userId and IsLeadInterviewer=1)
					Begin
						select fn.functionname,fn.description,fn.idparentfunction,fn.sicon,fn.surl,fn.idisplayorder,pl.Name profileName,    
						pf.dAccess,@lastLogin as NavigationShow,c.LogoLink from [dbo].[Function] fn      
						join [dbo].[ProfileFunction] pf on fn.id = pf.idfunction      
						join [dbo].[Profile] pl on pl.id = pf.idprofile      
						join [dbo].[UserList] ul on pl.id = ul.idprofile 
						left join ClientUser cu on cu.UserId=ul.id
						left join Client c on c.id=cu.ClientId
						where ul.EmailId LIKE @EmailId AND ul.IsActive=1 and (fn.IsUnapproved is null or fn.IsUnapproved=0) order by idisplayorder
					End
				ELSE IF Exists(select 1 from InterviewerProfile where UserId=@userId and IsApproved=0)
					Begin
						select fn.functionname,fn.description,fn.idparentfunction,fn.sicon,fn.surl,fn.idisplayorder,pl.Name profileName,    
						pf.dAccess,@lastLogin as NavigationShow,c.LogoLink from [dbo].[Function] fn      
						join [dbo].[ProfileFunction] pf on fn.id = pf.idfunction      
						join [dbo].[Profile] pl on pl.id = pf.idprofile      
						join [dbo].[UserList] ul on pl.id = ul.idprofile 
						left join ClientUser cu on cu.UserId=ul.id
						left join Client c on c.id=cu.ClientId
						where ul.EmailId LIKE @EmailId AND ul.IsActive=1 and (fn.IsUnapproved = 1 or fn.IsCommon=1)order by idisplayorder
					End
				ELSE
					Begin
						select fn.functionname,fn.description,fn.idparentfunction,fn.sicon,fn.surl,fn.idisplayorder,pl.Name profileName,    
						pf.dAccess,@lastLogin as NavigationShow,c.LogoLink from [dbo].[Function] fn      
						join [dbo].[ProfileFunction] pf on fn.id = pf.idfunction      
						join [dbo].[Profile] pl on pl.id = pf.idprofile      
						join [dbo].[UserList] ul on pl.id = ul.idprofile 
						left join ClientUser cu on cu.UserId=ul.id
						left join Client c on c.id=cu.ClientId
						where ul.EmailId LIKE @EmailId AND ul.IsActive=1 and (fn.IsUnapproved is null or fn.IsUnapproved=0) and (fn.IsLead is null or fn.IsLead=0) order by idisplayorder
					End
			End
			else if (@profile = 'Client' and @roleId = 4)--staffing agency
			begin
				select fn.functionname,fn.description,fn.idparentfunction,fn.sicon,fn.surl,fn.idisplayorder,pl.Name profileName,    
					pf.dAccess,@lastLogin as NavigationShow,c.LogoLink from [dbo].[Function] fn      
					join [dbo].[ProfileFunction] pf on fn.id = pf.idfunction      
					join [dbo].[Profile] pl on pl.id = pf.idprofile      
					join [dbo].[UserList] ul on pl.id = ul.idprofile 
					left join ClientUser cu on cu.UserId=ul.id
					left join Client c on c.id=cu.ClientId
					where ul.EmailId LIKE @EmailId AND ul.IsActive=1 and fn.IsStaffingAgencyAccess=1 order by idisplayorder
			end
			else
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
			      
		end  
	else  
		begin  
			select distinct fn.functionname,fn.description,fn.idparentfunction,fn.sicon,fn.surl,fn.idisplayorder,pl.Name profileName,    
			pf.dAccess,@lastLogin as NavigationShow from [dbo].[Function] fn      
			join [dbo].[ProfileFunction] pf on fn.id = pf.idfunction      
			join [dbo].[Profile] pl on pl.id = pf.idprofile      
			join [dbo].[UserList] ul on pl.id = ul.idprofile    
			where pl.Name='ExternalUser'  
		end      
End   
  

GO 




