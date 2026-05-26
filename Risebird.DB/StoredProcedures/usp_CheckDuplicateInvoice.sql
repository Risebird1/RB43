IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CheckDuplicateInvoice'))
    DROP PROCEDURE dbo.usp_CheckDuplicateInvoice
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns existing Generated invoice (Status=1) for the given ClientId + BillingOwnerId + Month + Year.
-- Returns empty if no duplicate exists.
CREATE PROCEDURE usp_CheckDuplicateInvoice
    @ClientId       INT,
    @BillingOwnerId INT,
    @InvoiceMonth   INT,
    @InvoiceYear    INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1
        id,
        InvoiceNumber,
        Status,
        CreatedOn,
        CreatedBy
    FROM GeneratedInvoices
    WHERE ClientId       = @ClientId
      AND BillingOwnerId = @BillingOwnerId
      AND InvoiceMonth   = @InvoiceMonth
      AND InvoiceYear    = @InvoiceYear
      AND Status         = 1
END
GO
