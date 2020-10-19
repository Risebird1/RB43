  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateClient'))
BEGIN
    DROP PROCEDURE [dbo].[usp_UpdateClient]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
    
    
    
    
CREATE PROCEDURE [dbo].[usp_UpdateClient]    
    
@id int,    
@clientName varchar(150),    
@clientDescr varchar(250),    
@GST varchar(MAX),    
@officeAddress varchar(MAX),  
@Balance int,  
@PerSuccessRate int,
@Createdby varchar(200)
    
AS    
Begin    
Declare @InitialBalance int
Declare @Userid int
Select @Userid=id from USerList where Emailid=@Createdby
Select @InitialBalance=Balance from Client where  Id = @id   
UPDATE [dbo].[CLIENT]    
SET     
      [Name]=@clientName    
     ,[Description] = @clientDescr    
  ,[GST] = @GST    
  ,[OfficeAddress] = @officeAddress,  
  [Balance]=@Balance,  
  [PerSuccessRate]=@PerSuccessRate  
WHERE    
      Id = @id    
if(@Balance!=@InitialBalance)
Begin
Declare @difference int
set @difference=@Balance-@InitialBalance
if(@difference>0)
Begin
Insert Into ClientWallet values(@id,@Userid,@difference,0,Getdate(),'Update User',@Balance)
ENd
ELSe
Begin
Insert Into ClientWallet values(@id,@Userid,0,@difference,Getdate(),'Update User',@Balance)

END
END

End    
    
    GO
  