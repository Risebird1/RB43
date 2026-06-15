IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetUnpaidInvoices'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUnpaidInvoices
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetUnpaidInvoices
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        gi.id,
        gi.ClientId,
        ISNULL(c.Name, '')                                    AS ClientName,
        ISNULL(c.BillingModel, 0)                             AS BillingModel,
        gi.BillingOwnerId                                     AS BillingOwner,
        ISNULL(NULLIF(gi.BillingOwnerEmail, ''), ul.EmailId)  AS BillingOwnerEmail,
        gi.InvoiceNumber,
        gi.IRN,
        gi.InvoiceValue                                       AS GrossAmount,
        gi.Status,
        gi.CreatedBy                                          AS Owner,
        gi.LatestRemark,
        gi.UTRTrxn,
        ISNULL(c.BillingType, 0)                              AS BillingType,
        gi.InvoiceMonth,
        gi.InvoiceYear,
        gi.CreatedOn,
        gi.ModifiedOn
    FROM GeneratedInvoices gi
    LEFT JOIN Client   c  ON c.id  = gi.ClientId
    LEFT JOIN UserList ul ON ul.id = gi.BillingOwnerId
    WHERE gi.Status <> 3
    ORDER BY c.Name, gi.InvoiceYear DESC, gi.InvoiceMonth DESC, gi.id
END
GO
