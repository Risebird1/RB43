  
  
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
@Balance int,  
@PerSuccessRate int,
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
     ,[OfficeAddress],  
  [Balance],  
  [PerSuccessRate]  
     )  
       VALUES    
           (@clientName    
     ,@clientDescr    
     ,@GST    
     ,@officeAddress,  
  @Balance,  
  @PerSuccessRate)    
  Select @Clientid=id from Client where Name=@clientName and Description=@clientDescr
  Insert Into ClientWallet values(@Clientid,@Userid,@Balance,0,Getdate(),'FirstTime Entry',@Balance)
End    
    GO
  