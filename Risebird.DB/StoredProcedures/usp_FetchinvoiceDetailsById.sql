  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchinvoiceDetailsById'))
BEGIN
    DROP PROCEDURE [dbo].[usp_FetchinvoiceDetailsById]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchinvoiceDetailsById
@id int
AS
BEGIN
Select  ind.id,c.Name,irf.JobCountry,Count(*) as CompletedInterview,ind.AccountNumber,ind.AccountDetails,ind.PaymentStatus,ind.Comment,ind.ReferenceNumber,ind.Price from interviewComments ic join interviewmeetings im on im.id=ic.InterviewId left join IRF on irf.irfid=im.IRFId 
join Client c on c.id=irf.ClientId
join InvoiceDetails ind on ind.Clientid=irf.ClientId and ind.JobCountry=irf.JobCountry 
where ind.id=@id
group by ind.id,c.Name,irf.JobCountry,ind.AccountNumber,ind.AccountDetails,ind.PaymentStatus,ind.Comment,ind.ReferenceNumber,ind.Price
END