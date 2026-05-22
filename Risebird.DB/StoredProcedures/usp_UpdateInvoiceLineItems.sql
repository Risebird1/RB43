IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateInvoiceLineItems'))
    DROP PROCEDURE dbo.usp_UpdateInvoiceLineItems
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Updates UnitRate and LineTotal for a single line item.
-- Called once per edited line item row.
CREATE PROCEDURE usp_UpdateInvoiceLineItems
    @LineItemId INT,
    @UnitRate   DECIMAL(18,2),
    @LineTotal  DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE GeneratedInvoiceLineItems SET
        UnitRate  = @UnitRate,
        LineTotal = @LineTotal
    WHERE id = @LineItemId
END
GO
