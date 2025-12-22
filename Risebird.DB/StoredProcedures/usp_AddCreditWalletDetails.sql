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
@Credit int =null    
AS      
BEGIN      
Declare @CreatedOn Datetime=DateAdd(Minute,330,Getdate())    

Insert into Wallet(BillingType,ClientId,BillingOwnerId,CreatedBy,Credit,Debit,CreatedOn,Comment,Balance,InterviewId,IRFId,TransactionType)    
values(@BillingType,@Clientid,@BillingOwnerId,@CreatedBy,@Credit,0,@CreatedOn,'Credit',0,0,0,@TransactionType)    

END
GO