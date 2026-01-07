IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddCreditWalletDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_AddCreditWalletDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_AddCreditWalletDetails      
@ClientId int, 
@BillingType int,
@BillingOwnerId int=null,
@CreatedBy varchar(200)=null,      
@TransactionType varchar(50), --InterviewBilling,Manual      
@Credit int =null,
@CreatedOn datetime = null,
@TransactionRefNumber varchar(200) = null,
@InvoicePDF varchar(500) = null
AS      
BEGIN      
Declare @CreatedOnValue Datetime
If @CreatedOn IS NULL
    Set @CreatedOnValue = DateAdd(Minute,330,Getdate())    
Else
    Set @CreatedOnValue = @CreatedOn

Insert into Wallet(BillingType,ClientId,BillingOwnerId,CreatedBy,Credit,Debit,CreatedOn,Comment,Balance,InterviewId,IRFId,TransactionType,TransactionRefNumber,InvoicePDF)    
values(@BillingType,@Clientid,@BillingOwnerId,@CreatedBy,@Credit,0,@CreatedOnValue,'Credit',0,0,0,@TransactionType,@TransactionRefNumber,@InvoicePDF)    

END
GO