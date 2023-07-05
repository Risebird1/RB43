IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRevenueDetailsByDate'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRevenueDetailsByDate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetRevenueDetailsByDate       
@StartDate datetime,        
@EndDate datetime,    
@year int,    
@month int    
AS        
BEGIN        
Select distinct cl.id,cl.Name,Count(im.id) as TotalCount,cl.PerInterviewRate,cl.BillingType,Po.POPendingAmount,Inv.InvoiceAmount,cl.GSTApplicable,cl.PaymentTerm From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId         
join InterviewComments ic on ic.InterviewId=im.id join Client cl on IRF.ClientId=cl.id      
outer Apply (Select Sum(POPendingAmount) as POPendingAmount from Revenuedetails where Clientid=IRF.ClientId and Months=@month and Years=@year) Po    
outer Apply (Select Sum(InvoiceAmount) as InvoiceAmount from Revenuedetails where Clientid=IRF.ClientId and Months=@month and Years=@year) Inv    
where im.interviewDate>=@StartDate and im.interviewDate<@EndDate         
and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and IsNull(im.RedoInterview,0)=0                  
group by cl.id,cl.Name,cl.PerInterviewRate,cl.BillingType,Po.POPendingAmount,Inv.InvoiceAmount,cl.BillingType,cl.GSTApplicable,cl.PaymentTerm           
END    
     


     
Go