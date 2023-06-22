IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddOrUpdateRevenueStatusDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_AddOrUpdateRevenueStatusDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_AddOrUpdateRevenueStatusDetails    
@Clientid int,    
@year int,    
@month int,    
@billingType int,    
@Detailid varchar(10),    
@currentStatus int,    
@invoiceamount int,    
@poPending int,    
@Emailid varchar(150),
@invoicerealization int,
@invoiceSubmitted bit
AS    
BEGIN    
Declare @Count int    
Declare @id int    
Declare @Userid int    
Declare @IRFId varchar(10)    
Declare @BillingOwner int=0    
Declare @CurrentDate datetime =DATEADD(MINUTE, 330, Getdate())
Select @Userid=id from UserList where EmailId=@Emailid    
if(@billingType=3)    
BEGIN    
Select @Count=Count(*) from RevenueDetails where Clientid=@Clientid and Months=@month and Years=@year  
Select @id=id from RevenueDetails where Clientid=@Clientid and Months=@month and Years=@year   
END    
else if(@billingType=2)    
BEGIN    
Select @Count=Count(*) from RevenueDetails where Clientid=@Clientid and Months=@month and Years=@year and IRFId=@Detailid    
Select @id=id from RevenueDetails where Clientid=@Clientid and Months=@month and Years=@year and IRFId=@Detailid    
Set @IRFId=@Detailid    
END    
else    
BEGIN    
Select @Count=Count(*) from RevenueDetails where Clientid=@Clientid and Months=@month and Years=@year and BillingOwner=@Detailid    
Select @id=id from RevenueDetails where Clientid=@Clientid and Months=@month and Years=@year and BillingOwner=@Detailid    
Set @BillingOwner=@Detailid    
END    
if(@Count>0)    
BEGIN    
Update RevenueDetails set POPendingAmount=@poPending,InvoiceAmount=@invoiceamount,CurrentStatus=@currentStatus,ModifiedOn=@CurrentDate,MOdifiedBy=@Userid,invoicerealization=@invoicerealization,invoiceSubmitted=invoiceSubmitted,invoiceDate=@CurrentDate,invoiceSubmission=@CurrentDate where id=@id    
END    
else    
BEGIN    
Insert into RevenueDetails (Clientid,Months,Years,POPendingAmount,InvoiceAmount,CurrentStatus,CreatedOn,CreadtedBy,    
ModifiedOn,MOdifiedBy,IRFId,BillingType,BillingOwner,invoicerealization,invoiceSubmitted,invoiceDate,invoiceSubmission) values(@Clientid,@month,@year,@poPending,@invoiceamount,@currentStatus,@CurrentDate,@Userid,    
@CurrentDate,@Userid,@IRFId,@billingType,@BillingOwner,@invoicerealization,@invoiceSubmitted,@CurrentDate,@CurrentDate)    
END    
END


GO