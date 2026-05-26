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
@TransactionType varchar(50), -- CashCredit, Manual, InterviewBilling      
@Credit int =null,
@CreatedOn datetime = null,
@TransactionDate datetime = null,
@TransactionRefNumber varchar(200) = null,
@InvoicePDF varchar(500) = null,
@ClientPOId int = null
AS      
BEGIN      
Declare @CreatedOnValue Datetime = DateAdd(Minute,330,Getdate())
Declare @TransactionDateValue Datetime = @TransactionDate

Insert into Wallet(BillingType,ClientId,BillingOwnerId,CreatedBy,Credit,Debit,CreatedOn,Comment,Balance,InterviewId,IRFId,TransactionType,TransactionDate,TransactionRefNumber,InvoicePDF,ClientPOId)    
values(@BillingType,@Clientid,@BillingOwnerId,@CreatedBy,@Credit,0,@CreatedOnValue,'Credit',0,0,0,@TransactionType,@TransactionDateValue,@TransactionRefNumber,@InvoicePDF,@ClientPOId)    

-- When cash credit is applied against a PO, update PO cash received status
IF @ClientPOId IS NOT NULL AND ISNULL(@Credit, 0) > 0
BEGIN
    UPDATE ClientPO
    SET CashCreditReceived = CashCreditReceived + @Credit,
        ModifiedOn = @CreatedOnValue,
        Status = CASE
            WHEN CashCreditReceived + @Credit >= POCreditAmount AND POCreditAmount > 0 THEN 2  -- CashCredited
            WHEN CashCreditReceived + @Credit > 0 THEN 1                                       -- PartialCash
            ELSE Status
        END
    WHERE id = @ClientPOId
      AND ClientId = @ClientId
END
END
GO
