  
  
  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddClient'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddClient]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

    
    
    
Create PROCEDURE [dbo].[usp_AddClient]    
    
@clientName varchar(150),    
@clientDescr varchar(250),    
@GST varchar(MAX),    
@officeAddress varchar(MAX),  
@Createdby varchar(200),
@LogoLink varchar(max),
@CompanyLink varchar(max),
@CompanyVideoLink varchar(max),
@IsPasswordReset bit
    
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
		 )  
		   VALUES    
					(@clientName    
					,@clientDescr    
					,@GST    
					,@officeAddress
					,@LogoLink
					,@CompanyLink
					,@CompanyVideoLink
					,@IsPasswordReset)    

End     

GO
  