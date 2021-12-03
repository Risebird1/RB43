IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddinterviewvedioSharedetails'))
BEGIN
    DROP PROCEDURE dbo.usp_AddinterviewvedioSharedetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure [dbo].[usp_AddinterviewvedioSharedetails]    
@Email varchar(max),    
@VedioId int,  
@EmailAddress varchar(100),
@uniqueid varchar(50)
AS    
Begin    
Declare @emailUser varchar(100)
SELECT @emailUser= value FROM STRING_SPLIT(@Email,'@') ORDER BY (SELECT NULL)
OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY
declare @profileid int
select @profileid= id from profile where name like 'ExternalUser'
Declare @Icount int  
Declare @id int  
Select @id= Id from UserList where EmailId=@Email       
select @Icount=Count(*) from UserList where EmailId=@Email    
if(@Icount=0)  
Begin  
Insert into UserList(idProfile,FirstName,LastName,EmailId,LastLogin,LastLogout,IsActive,uniqueid)
values(@profileid,@emailUser,@emailUser,@Email,null,null,1,@uniqueid) 
Select @id= Id from UserList where EmailId=@Email     
Insert into tblsharedvideo values(@id,@VedioId,@EmailAddress,GETDATE())   
END
else 
begin
Insert into tblsharedvideo values(@id,@VedioId,@EmailAddress,GETDATE())    
Select @Email
end 

END 
go

