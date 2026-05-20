IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInvoiceList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInvoiceList
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetInvoiceList
    @Month INT,
    @Year  INT
AS
BEGIN
    SELECT
        cil.id,
        c.id        AS ClientId,
        c.Name      AS ClientName,
        cil.InvoiceModel,
        ul.EmailId  AS BillingOwnerEmail,
        cil.BillingOwner,
        cil.InvoiceNumber,
        cil.IRN,
        cil.GrossAmount,
        cil.Status,
        cil.Owner,
        cil.LatestRemark,
        cil.UTRTrxn,
        cil.BillingType,
        cil.InvoiceMonth,
        cil.InvoiceYear,
        cil.CreatedOn,
        cil.ModifiedOn
    FROM ClientInvoiceList cil
    LEFT JOIN Client c    ON c.id = cil.ClientId
    LEFT JOIN UserList ul ON ul.id = cil.BillingOwner
    WHERE cil.InvoiceMonth = @Month
      AND cil.InvoiceYear  = @Year
    ORDER BY c.Name, cil.id
END
GO
