  
  
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
@Createdby varchar(200)
    
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
     )  
       VALUES    
           (@clientName    
     ,@clientDescr    
     ,@GST    
     ,@officeAddress)    

End    
    GO
  