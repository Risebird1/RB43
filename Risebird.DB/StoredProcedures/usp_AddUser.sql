/****** Object:  StoredProcedure [dbo].[usp_AddUser]    Script Date: 3/26/2021 10:42:57 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddUser'))
BEGIN
    DROP PROCEDURE dbo.usp_AddUser
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO 
CREATE PROCEDURE [dbo].[usp_AddUser]          
          
@selectedProfileId int,          
@userFirstName varchar(150),          
@userLastName varchar(150),          
@userEmailId varchar(150),          
@userMobileNumber nvarchar(200),    
@WhatsappNumber varchar(15) 
          
AS          
Begin          
          
declare @generatedUserId int;          
    INSERT INTO [dbo].[UserList]          
         (          
       [idProfile]          
         ,[FirstName]               
         ,[LastName]          
         ,[EmailId]          
         ,[IsActive]          
         ,[MobileNumber],    
   [WhatsappNumber],  
   [uniqueid]  
         )          
        VALUES          
         (          
       @selectedProfileId          
         ,@userFirstName          
         ,@userLastName          
         ,@userEmailId          
         ,1          
         ,@userMobileNumber,    
   @WhatsappNumber,  
   NEWID()  
         )       
      
 set @generatedUserId=SCOPE_IDENTITY()      
      
--check if HR         
 if(@selectedProfileId=2)      
  Begin      
   insert into HR values(@generatedUserId)      
  End      
          
 Select  @generatedUserId        
          
End      
    
GO


