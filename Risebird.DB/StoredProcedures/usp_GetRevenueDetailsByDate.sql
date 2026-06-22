IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRevenueDetailsByDate'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRevenueDetailsByDate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetRevenueDetailsByDate       
@StartDate  datetime,        
@EndDate    datetime,    
@FromYear   int,    
@FromMonth  int,
@ToYear     int,
@ToMonth    int
AS        
BEGIN        
Select distinct cl.id, cl.Name,
    Count(im.id) as TotalCount,
    SUM(CASE WHEN ISNULL(IRF.IsAIInterview, 0) = 0 THEN 1 ELSE 0 END) AS NonAIInterviewCount,
    SUM(CASE WHEN ISNULL(IRF.IsAIInterview, 0) = 1 THEN 1 ELSE 0 END) AS AIInterviewCount,
    cl.PerInterviewRate, cl.AIPerInterviewRate, cl.BillingType,
    Po.POPendingAmount, Inv.InvoiceAmount, cl.GSTApplicable, cl.PaymentTerm,
    ISNULL(GI.GenInvoiceAmount, 0)  AS GeneratedInvoiceAmount,
    ISNULL(GI.GenInvoiceCount, 0)   AS GeneratedInvoiceCount,
    ISNULL(BOC.TotalBillingOwnerCount, 1) AS TotalBillingOwnerCount,
    ISNULL(PI.PaidInvoiceAmount, 0) AS ReceivedAmount
From InterviewMeetings im
join IRF on IRF.IRFId = im.IRFId         
join InterviewComments ic on ic.InterviewId = im.id
join Client cl on IRF.ClientId = cl.id      
outer Apply (
    Select Sum(POPendingAmount) as POPendingAmount
    from Revenuedetails
    where Clientid = IRF.ClientId
      and (Years * 100 + Months) BETWEEN (@FromYear * 100 + @FromMonth) AND (@ToYear * 100 + @ToMonth)
) Po    
outer Apply (
    Select Sum(InvoiceAmount) as InvoiceAmount
    from Revenuedetails
    where Clientid = IRF.ClientId
      and (Years * 100 + Months) BETWEEN (@FromYear * 100 + @FromMonth) AND (@ToYear * 100 + @ToMonth)
) Inv
outer Apply (
    Select ISNULL(Sum(gi.InvoiceValue), 0) as GenInvoiceAmount,
           COUNT(gi.id)                    as GenInvoiceCount
    from GeneratedInvoices gi
    where gi.ClientId = cl.id
      and (gi.InvoiceYear * 100 + gi.InvoiceMonth) BETWEEN (@FromYear * 100 + @FromMonth) AND (@ToYear * 100 + @ToMonth)
      and gi.Status > 0
) GI
outer Apply (
    Select COUNT(distinct IRF2.BillingOwner) as TotalBillingOwnerCount
    from InterviewMeetings im2
    join IRF IRF2 on IRF2.IRFId = im2.IRFId
    join InterviewComments ic2 on ic2.InterviewId = im2.id
    where im2.interviewDate >= @StartDate
      and im2.interviewDate <  @EndDate
      and IRF2.ClientId = cl.id
      and (ic2.MeetingStatus = 4 or ic2.MeetingStatus = 6 or ic2.MeetingStatus = 7 or ic2.MeetingStatus = 8)
      and ISNULL(im2.RedoInterview, 0) = 0
) BOC
outer Apply (
    Select ISNULL(Sum(gi2.InvoiceValue), 0) as PaidInvoiceAmount
    from GeneratedInvoices gi2
    where gi2.ClientId = cl.id
      and (gi2.InvoiceYear * 100 + gi2.InvoiceMonth) BETWEEN (@FromYear * 100 + @FromMonth) AND (@ToYear * 100 + @ToMonth)
      and gi2.Status = 5
) PI
where im.interviewDate >= @StartDate and im.interviewDate < @EndDate         
and (ic.MeetingStatus = 4 or ic.MeetingStatus = 6 or ic.MeetingStatus = 7 or ic.MeetingStatus = 8)
and IsNull(im.RedoInterview, 0) = 0                  
group by cl.id, cl.Name, cl.PerInterviewRate, cl.AIPerInterviewRate, cl.BillingType,
    Po.POPendingAmount, Inv.InvoiceAmount, cl.GSTApplicable, cl.PaymentTerm,
    GI.GenInvoiceAmount, GI.GenInvoiceCount, BOC.TotalBillingOwnerCount,
    PI.PaidInvoiceAmount
END    
     


     
Go
