IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddWalletPoints'))
BEGIN
    DROP PROCEDURE dbo.usp_AddWalletPoints
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
 
CREATE Procedure usp_AddWalletPoints      
@ClientId int,    
@emailid varchar(150),    
@points int,
@comment varchar(Max)
AS      
BEGIN      
Declare @Userid int     
Declare @Comments varchar(max)    
Declare @Balance int    
Select @Userid=id from UserList where Emailid=@emailid    
if(IsNull(@comment,'')='')
BEGIN
Set @Comments='Points added by '+ @emailid   
END
ELSE
BEGIN
Set @Comments=@comment
END
Select @Balance=(sum(Credit)-sum(debit)+(@points)) from ClientWallet where Clientid=@ClientId      
Insert Into ClientWallet values(@ClientId,@Userid,@points,0,Getdate(),@Comments,@Balance)    
Update Client Set Balance=@Balance where id=@ClientId  
END 
GO