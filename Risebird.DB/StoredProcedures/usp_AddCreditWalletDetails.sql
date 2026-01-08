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
@TransactionDate datetime = null,
@TransactionRefNumber varchar(200) = null,
@InvoicePDF varchar(500) = null
AS      
BEGIN      
-- CreatedOn is always set to current timestamp (when record is created)
Declare @CreatedOnValue Datetime = DateAdd(Minute,330,Getdate())

-- TransactionDate is separate and can be different from CreatedOn
-- It can be null if not provided by the user
Declare @TransactionDateValue Datetime = @TransactionDate

Insert into Wallet(BillingType,ClientId,BillingOwnerId,CreatedBy,Credit,Debit,CreatedOn,Comment,Balance,InterviewId,IRFId,TransactionType,TransactionDate,TransactionRefNumber,InvoicePDF)    
values(@BillingType,@Clientid,@BillingOwnerId,@CreatedBy,@Credit,0,@CreatedOnValue,'Credit',0,0,0,@TransactionType,@TransactionDateValue,@TransactionRefNumber,@InvoicePDF)    

END
GO