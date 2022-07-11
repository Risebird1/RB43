IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRevenueStatusDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRevenueStatusDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetRevenueStatusDetails    
@Clientid int,    
@year int,    
@month int,    
@billingType int,    
@Detailid varchar(10)    
AS    
BEGIN    
if(@billingType=3)    
BEGIN   
Select CurrentStatus,POPendingAmount,InvoiceAmount,invoicerealization from RevenueDetails where Clientid=@Clientid and Months=@month and Years=@year  
END  
else if(@billingType=2)    
BEGIN    
Select CurrentStatus,POPendingAmount,InvoiceAmount,invoicerealization from RevenueDetails where Clientid=@Clientid and Months=@month and Years=@year and IRFId=@Detailid    
END    
else    
BEGIN    
Select CurrentStatus,POPendingAmount,InvoiceAmount,invoicerealization from RevenueDetails where Clientid=@Clientid and Months=@month and Years=@year and BillingOwner=@Detailid    
END    
END
GO