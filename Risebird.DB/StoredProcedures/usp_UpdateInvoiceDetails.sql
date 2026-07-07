IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateInvoiceDetails'))
    DROP PROCEDURE dbo.usp_UpdateInvoiceDetails
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Updates editable fields + recalculated financial totals on an existing invoice.
CREATE PROCEDURE usp_UpdateInvoiceDetails
    @InvoiceId         INT,
    @GSTNumber         VARCHAR(100),
    @CreditPeriod      VARCHAR(100),
    @IRN               VARCHAR(200),
    @EInvoiceFileLink  VARCHAR(MAX),
    @Status            INT,
    @Subtotal          DECIMAL(18,2),
    @GSTAmount         DECIMAL(18,2),
    @InvoiceValue      DECIMAL(18,2),
    @TDSAmount         DECIMAL(18,2),
    @NetReceivable     DECIMAL(18,2),
    @LatestRemark      VARCHAR(500) = NULL,
    @UTRTrxn           VARCHAR(200) = NULL,
    @CashCreditAmount  DECIMAL(18,2) = NULL,
    @TaxScenario       INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE GeneratedInvoices SET
        GSTNumber        = @GSTNumber,
        CreditPeriod     = @CreditPeriod,
        IRN              = @IRN,
        EInvoiceFileLink = CASE WHEN @EInvoiceFileLink IS NOT NULL AND @EInvoiceFileLink != '' THEN @EInvoiceFileLink ELSE EInvoiceFileLink END,
        Status           = @Status,
        Subtotal         = @Subtotal,
        GSTAmount        = @GSTAmount,
        InvoiceValue     = @InvoiceValue,
        TDSAmount        = @TDSAmount,
        NetReceivable    = @NetReceivable,
        LatestRemark        =  @LatestRemark,
        UTRTrxn             = CASE WHEN @UTRTrxn IS NOT NULL AND @UTRTrxn != '' THEN @UTRTrxn ELSE UTRTrxn END,
        CashCreditAmount    = @CashCreditAmount,
        TaxScenario         = CASE WHEN @TaxScenario != 0 THEN @TaxScenario ELSE TaxScenario END,
        InvoiceBaseAmount   = @Subtotal,
        NetReceivableAmount = @NetReceivable,
        ModifiedOn          = GETDATE()
    WHERE id = @InvoiceId

    SELECT @@ROWCOUNT AS UpdatedRows
END
GO
