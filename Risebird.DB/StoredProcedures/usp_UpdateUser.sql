/****** Object:  StoredProcedure [dbo].[usp_UpdateUser]    Script Date: 4/1/2021 12:55:25 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateUser'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateUser
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
  
    
Create PROCEDURE [dbo].[usp_UpdateUser]    
@id INT,    
@selectedProfileId int,    
@userFirstName varchar(150),       
@userLastName varchar(150),    
@userEmailId varchar(150),    
@userMobileNumber nvarchar(200),  
@WhatsappNumber varchar(15),
@IsActive bit
AS     
Begin    
    
IF EXISTS( SELECT 1 FROM UserList WHERE id = @id )      
       BEGIN      
            UPDATE UserList SET idProfile = @selectedProfileId,      
            FirstName =  @userFirstName,       
            LastName =  @userLastName,      
            EmailID =   @userEmailId,      
            MobileNumber = @userMobileNumber,  
   WhatsappNumber=@WhatsappNumber,
   IsActive=@IsActive
            WHERE id = @id      
                          
       END    
if(@selectedProfileId=2)    
 Begin    
  IF NOT Exists( select 1 from HR where UserId=@id)    
  Begin    
   insert into HR values (@id)    
  End    
    
 End    
else if(@selectedProfileId != 2 and ((select id from HR where UserId=@id) is not null) )    
 Begin    
  delete from TransHR where HRId = (select id from HR where UserId=@id)    
  delete from HR where id = (select id from HR where UserId=@id)    
 End    
    Delete from SeniorHR where Userid=@id  
End
    
  
GO

