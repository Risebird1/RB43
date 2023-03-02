
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchinvoiceDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchinvoiceDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_FetchinvoiceDetails  
@Startdate DateTime,
@Enddate DateTime
AS
BEGIN
Select  ind.id,c.Name,irf.JobCountry,Count(*) as CompletedInterview,ind.AccountNumber,ind.AccountDetails,ind.PaymentStatus,ind.Comment,ind.ReferenceNumber,ind.Price,ind.UploadedFiles as filelink from interviewComments ic join interviewmeetings im on im.id=ic.InterviewId left join IRF on irf.irfid=im.IRFId 
join Client c on c.id=irf.ClientId
left join InvoiceDetails ind on ind.Clientid=irf.ClientId and ind.JobCountry=irf.JobCountry and ind.Startdate>=@Startdate and ind.EndDate<=@Enddate
where (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and im.interviewDate>=@Startdate and im.interviewDate<=@Enddate
group by ind.id,c.Name,irf.JobCountry,ind.AccountNumber,ind.AccountDetails,ind.PaymentStatus,ind.Comment,ind.ReferenceNumber,ind.Price,ind.UploadedFiles
END
GO