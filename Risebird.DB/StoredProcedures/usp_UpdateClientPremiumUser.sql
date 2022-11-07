/****** Object:  StoredProcedure [dbo].[usp_UpdateClientUser]    Script Date: 03-09-2020 2.27.02 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateClientPremiumUser'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateClientPremiumUser
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
Create PROCEDURE [dbo].[usp_UpdateClientPremiumUser]  
(@id INT,   
@userFirstName varchar(150),   
@userLastName varchar(150),   
@userEmailId varchar(150),   
@userMobileNumber nvarchar(200),  
@UserPassword nvarchar(60),
@WhatsappNumber varchar(15),
@MCQValidation int)    
AS    
Begin Try    
  BEGIN TRAN UpdateUser     
       Declare @firstName varchar(150)    
       Declare @lastName varchar(150)    
       Declare @email varchar(150)    
       Declare @phone varchar(150)    
       Declare @cuid int    
    Declare @roleId int    
    Declare @userId int  
    Declare @password nvarchar(60)    
  
    
       SET @firstName = @userFirstName    
       SET @lastName = @userLastName    
       SET @email = @userEmailId    
       SET @phone = @userMobileNumber    
       SET @cuid = @id    
    set @password = @UserPassword  
    
       IF EXISTS( SELECT 1 FROM UserList ul JOIN [dbo].[ClientPremiumUSer] cu on ul.id = cu.UserId where cu.id = @cuid )    
       BEGIN    
    set @userId= (select userid from ClientPremiumUser where id=@cuid)  
             UPDATE UserList SET FirstName =  @firstName,     
                           LastName =  @lastName,    
                           EmailID =   @email,    
         MobileNumber = @phone,   
         UserPassword =@password,
		 WhatsappNumber=@WhatsappNumber
    where id= @userId   
     update ClientPremiumUser set MCQValidation=@MCQValidation where id=@cuid
       END    
     
 Commit TRAN UpdateUser;    
End Try     
Begin Catch    
 RAISERROR ('Error While Updating Client User Details', -- Message text.    
   16, -- Severity.    
   1 -- State.    
   );    
    
 Rollback;    
End Catch    
   
GO


