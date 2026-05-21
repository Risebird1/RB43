IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveGeneratedInvoice'))
    DROP PROCEDURE dbo.usp_SaveGeneratedInvoice
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_SaveGeneratedInvoice
    @id                INT,
    @ClientId          INT,
    @InvoiceNumber     VARCHAR(50),
    @InvoiceMonth      INT,
    @InvoiceYear       INT,
    @BillingOwnerEmail VARCHAR(200),
    @CreditPeriod      VARCHAR(100),
    @GSTNumber         VARCHAR(100),
    @TaxScenario       INT,
    @IRN               VARCHAR(200),
    @Subtotal          DECIMAL(18,2),
    @GSTAmount         DECIMAL(18,2),
    @InvoiceValue      DECIMAL(18,2),
    @TDSAmount         DECIMAL(18,2),
    @NetReceivable     DECIMAL(18,2),
    @Status            INT,
    @CreatedBy         VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    IF @id = 0
    BEGIN
        INSERT INTO GeneratedInvoices
            (ClientId, InvoiceNumber, InvoiceMonth, InvoiceYear, BillingOwnerEmail,
             CreditPeriod, GSTNumber, TaxScenario, IRN,
             Subtotal, GSTAmount, InvoiceValue, TDSAmount, NetReceivable,
             Status, CreatedOn, CreatedBy)
        VALUES
            (@ClientId, @InvoiceNumber, @InvoiceMonth, @InvoiceYear, @BillingOwnerEmail,
             @CreditPeriod, @GSTNumber, @TaxScenario, @IRN,
             @Subtotal, @GSTAmount, @InvoiceValue, @TDSAmount, @NetReceivable,
             @Status, GETDATE(), @CreatedBy)

        SELECT SCOPE_IDENTITY() AS NewId
    END
    ELSE
    BEGIN
        UPDATE GeneratedInvoices SET
            ClientId          = @ClientId,
            InvoiceNumber     = @InvoiceNumber,
            InvoiceMonth      = @InvoiceMonth,
            InvoiceYear       = @InvoiceYear,
            BillingOwnerEmail = @BillingOwnerEmail,
            CreditPeriod      = @CreditPeriod,
            GSTNumber         = @GSTNumber,
            TaxScenario       = @TaxScenario,
            IRN               = @IRN,
            Subtotal          = @Subtotal,
            GSTAmount         = @GSTAmount,
            InvoiceValue      = @InvoiceValue,
            TDSAmount         = @TDSAmount,
            NetReceivable     = @NetReceivable,
            Status            = @Status,
            ModifiedOn        = GETDATE()
        WHERE id = @id

        SELECT @id AS NewId
    END
END
GO
