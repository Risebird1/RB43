IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddPremiumClientUser'))
BEGIN
    DROP PROCEDURE dbo.usp_AddPremiumClientUser
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddPremiumClientUser]              
(@selectedClientId int,              
@userFirstName varchar(150),              
@userLastName varchar(150),              
@userEmailId varchar(150),              
@userMobileNumber nvarchar(200),          
@UserPassword nvarchar(60),         
@WhatsappNumber varchar(15),     
@message varchar(100) OUTPUT)              
              
AS              
Begin              
              
declare @generatedUserId int;              
              
Begin Try              
              
 Begin Tran AddClientUser              
              
  IF NOT EXISTS( SELECT 1 FROM [UserList] Where [EmailId] = @userEmailId)               
  BEGIN              
   INSERT INTO [UserList] ([idProfile],[FirstName],[LastName],[EmailId],[IsActive],[MobileNumber],[UserPassword],[WhatsappNumber],[PasswordCreation],[uniqueid])              
          VALUES (33,@userFirstName,@userLastName,@userEmailId,1,@userMobileNumber,@UserPassword,@WhatsappNumber,GetDate(),NEWID() )              
              
   set @generatedUserId = SCOPE_IDENTITY();              
              
   INSERT INTO [dbo].[ClientPremiumUser] ([ClientId],[UserId])              
        VALUES (@selectedClientId ,@generatedUserId)              
  END              
  ELSE              
  BEGIN              
   SET @message ='Duplicate Email ID'              
  END              
                
 Commit Tran AddClientUser              
              
End Try              
                
Begin Catch              
                
 RAISERROR ('Error While Data Insertion ClientUser', -- Message text.              
   16, -- Severity.              
   1 -- State.              
   );              
              
 Rollback;              
              
End Catch              
              
End        
Go  