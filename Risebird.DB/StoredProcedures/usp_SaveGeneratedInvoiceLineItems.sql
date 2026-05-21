IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveGeneratedInvoiceLineItems'))
    DROP PROCEDURE dbo.usp_SaveGeneratedInvoiceLineItems
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_SaveGeneratedInvoiceLineItems
    @InvoiceId   INT,
    @ProductName VARCHAR(200),
    @Description VARCHAR(500),
    @Qty         INT,
    @UnitRate    DECIMAL(18,2),
    @LineTotal   DECIMAL(18,2),
    @Basis       VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO GeneratedInvoiceLineItems
        (InvoiceId, ProductName, Description, Qty, UnitRate, LineTotal, Basis)
    VALUES
        (@InvoiceId, @ProductName, @Description, @Qty, @UnitRate, @LineTotal, @Basis)
END
GO
