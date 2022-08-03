  
  
  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddClient'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddClient]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_AddClient]          
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
@IsMeetingNotificationSend bit,    
@PerInterviewRate int,
@BillingType int
AS          
Begin          
 Declare @Clientid int        
 Declare @Userid int      
 Select @Userid=id from USerList where Emailid=@Createdby      
 INSERT INTO [dbo].[Client]          
       ([Name]          
       ,[Description]          
       ,[GST]          
       ,[OfficeAddress]       
       ,[LogoLink]      
       ,[CompanyLink]      
       ,[CompanyVideoLink]      
       ,[IsPasswordReset]    
    ,[IsCandidateavailability]    
    ,[IsMeetingNotificationSend]   
 ,[PerInterviewRate]
 ,[BillingType])        
     VALUES          
     (@clientName          
     ,@clientDescr          
     ,@GST          
     ,@officeAddress      
     ,@LogoLink      
     ,@CompanyLink      
     ,@CompanyVideoLink      
     ,@IsPasswordReset    
  ,@IsCandidateavailability    
  ,@IsMeetingNotificationSend  
  ,@PerInterviewRate
  ,@BillingType)          
      
set @Clientid=SCOPE_IDENTITY()
Insert into SeniorHR Select id as Userid,@Clientid as Clientid from UserList where idprofile=32
End           
GO
  