IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInvoiceList'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInvoiceList
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_SaveInvoiceList
    @id            INT,
    @ClientId      INT,
    @InvoiceModel  VARCHAR(100),
    @BillingType   INT,
    @BillingOwner  INT,
    @InvoiceNumber VARCHAR(100),
    @IRN           VARCHAR(200),
    @GrossAmount   DECIMAL(18,2),
    @Status        INT,
    @Owner         VARCHAR(200),
    @LatestRemark  VARCHAR(500),
    @UTRTrxn       VARCHAR(200),
    @InvoiceMonth  INT,
    @InvoiceYear   INT
AS
BEGIN
    IF @id = 0
    BEGIN
        INSERT INTO ClientInvoiceList
            (ClientId, InvoiceModel, BillingType, BillingOwner, InvoiceNumber, IRN, GrossAmount, Status, Owner, LatestRemark, UTRTrxn, InvoiceMonth, InvoiceYear, CreatedOn)
        VALUES
            (@ClientId, @InvoiceModel, @BillingType, @BillingOwner, @InvoiceNumber, @IRN, @GrossAmount, @Status, @Owner, @LatestRemark, @UTRTrxn, @InvoiceMonth, @InvoiceYear, GETDATE())
    END
    ELSE
    BEGIN
        UPDATE ClientInvoiceList SET
            ClientId      = @ClientId,
            InvoiceModel  = @InvoiceModel,
            BillingType   = @BillingType,
            BillingOwner  = @BillingOwner,
            InvoiceNumber = @InvoiceNumber,
            IRN           = @IRN,
            GrossAmount   = @GrossAmount,
            Status        = @Status,
            Owner         = @Owner,
            LatestRemark  = @LatestRemark,
            UTRTrxn       = @UTRTrxn,
            InvoiceMonth  = @InvoiceMonth,
            InvoiceYear   = @InvoiceYear,
            ModifiedOn    = GETDATE()
        WHERE id = @id
    END
END
GO
