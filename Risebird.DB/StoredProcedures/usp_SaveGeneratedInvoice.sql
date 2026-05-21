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
    @BillingOwnerId    INT,
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

    -- Duplicate check: ClientId + BillingOwnerId + Month + Year must be unique for Generated invoices.
    -- Allow saving a Draft (Status=0) freely; block a second Generated (Status=1) invoice.
    IF @id = 0 AND @Status = 1
    BEGIN
        IF EXISTS (
            SELECT 1 FROM GeneratedInvoices
            WHERE ClientId      = @ClientId
              AND BillingOwnerId = @BillingOwnerId
              AND InvoiceMonth  = @InvoiceMonth
              AND InvoiceYear   = @InvoiceYear
              AND Status        = 1
        )
        BEGIN
            SELECT -1 AS NewId, 'Invoice already generated for this Client, Billing Owner, Month and Year.' AS ErrorMessage
            RETURN
        END
    END

    IF @id = 0
    BEGIN
        INSERT INTO GeneratedInvoices
            (ClientId, BillingOwnerId, InvoiceNumber, InvoiceMonth, InvoiceYear,
             BillingOwnerEmail, CreditPeriod, GSTNumber, TaxScenario, IRN,
             Subtotal, GSTAmount, InvoiceValue, TDSAmount, NetReceivable,
             Status, CreatedOn, CreatedBy)
        VALUES
            (@ClientId, @BillingOwnerId, @InvoiceNumber, @InvoiceMonth, @InvoiceYear,
             @BillingOwnerEmail, @CreditPeriod, @GSTNumber, @TaxScenario, @IRN,
             @Subtotal, @GSTAmount, @InvoiceValue, @TDSAmount, @NetReceivable,
             @Status, GETDATE(), @CreatedBy)

        SELECT SCOPE_IDENTITY() AS NewId, '' AS ErrorMessage
    END
    ELSE
    BEGIN
        UPDATE GeneratedInvoices SET
            ClientId          = @ClientId,
            BillingOwnerId    = @BillingOwnerId,
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

        SELECT @id AS NewId, '' AS ErrorMessage
    END
END
GO
