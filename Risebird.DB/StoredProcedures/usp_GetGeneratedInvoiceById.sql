IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetGeneratedInvoiceById'))
    DROP PROCEDURE dbo.usp_GetGeneratedInvoiceById
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns full details of a single generated invoice including its line items.
CREATE PROCEDURE usp_GetGeneratedInvoiceById
    @InvoiceId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Invoice header
    SELECT
        gi.id,
        gi.ClientId,
        ISNULL(c.Name, '')                                    AS ClientName,
        gi.BillingOwnerId,
        ISNULL(NULLIF(gi.BillingOwnerEmail, ''), ul.EmailId)  AS BillingOwnerEmail,
        gi.InvoiceNumber,
        gi.InvoiceMonth,
        gi.InvoiceYear,
        gi.CreditPeriod,
        gi.GSTNumber,
        gi.TaxScenario,
        gi.IRN,
        gi.Subtotal,
        gi.GSTAmount,
        gi.InvoiceValue,
        gi.TDSAmount,
        gi.NetReceivable,
        gi.Status,
        gi.CreatedBy,
        gi.CreatedOn,
        gi.ModifiedOn,
        ISNULL(c.BillingType, 0)                              AS BillingType
    FROM GeneratedInvoices gi
    LEFT JOIN Client   c  ON c.id  = gi.ClientId
    LEFT JOIN UserList ul ON ul.id = gi.BillingOwnerId
    WHERE gi.id = @InvoiceId

    -- Line items
    SELECT
        id,
        InvoiceId,
        ProductName,
        Description,
        Qty,
        UnitRate,
        LineTotal,
        Basis
    FROM GeneratedInvoiceLineItems
    WHERE InvoiceId = @InvoiceId
    ORDER BY id
END
GO
