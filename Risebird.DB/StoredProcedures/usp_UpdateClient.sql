  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateClient'))
BEGIN
    DROP PROCEDURE [dbo].[usp_UpdateClient]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_UpdateClient]      
      
@id int,      
@clientName varchar(150),      
@clientDescr varchar(250),      
@GST varchar(MAX),      
@officeAddress varchar(MAX),  
@Createdby varchar(200),  
@LogoLink varchar(max),  
@CompanyLink varchar(max),  
@CompanyVideoLink varchar(max),  
@IsPasswordReset bit,
@IsCandidateavailability bit,
@IsMeetingNotificationSend bit
      
AS      
Begin      
  
Declare @Userid int  
Select @Userid=id from USerList where Emailid=@Createdby  
  
UPDATE [dbo].[CLIENT]      
SET       
  [Name]=@clientName      
  ,[Description] = @clientDescr      
  ,[GST] = @GST      
  ,[OfficeAddress] = @officeAddress  
  ,[LogoLink]=@LogoLink  
  ,[CompanyLink]=@CompanyLink  
  ,[CompanyVideoLink]=@CompanyVideoLink  
  ,[IsPasswordReset] = @IsPasswordReset
  ,[IsCandidateavailability]=@IsCandidateavailability
  ,[IsMeetingNotificationSend]=@IsMeetingNotificationSend
WHERE      
      Id = @id      
  
  
End        
       
    
    GO
  