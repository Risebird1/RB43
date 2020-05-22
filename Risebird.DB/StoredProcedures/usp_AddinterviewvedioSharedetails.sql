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
@EmailAddress varchar(100)  
AS    
Begin    
Declare @Icount int  
Declare @id int  
Select @id= Id from UserList where EmailId=@Email       
select @Icount=Count(*) from UserList where EmailId=@Email    
if(@Icount=0)  
Begin  
Insert into UserList values(27,'NewUser','NewUser',@Email,null,null,1,null,null) 
Select @id= Id from UserList where EmailId=@Email     
Insert into tblsharedvideo values(@id,@VedioId,@EmailAddress) 
END
else 
begin
Insert into tblsharedvideo values(@id,@VedioId,@EmailAddress) 
Select @Email
end 

END    

