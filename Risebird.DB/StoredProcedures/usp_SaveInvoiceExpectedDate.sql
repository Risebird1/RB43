IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInvoiceExpectedDate'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInvoiceExpectedDate
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_SaveInvoiceExpectedDate
    @InvoiceId   INT,
    @ExpectedDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE GeneratedInvoices
    SET    ExpectedDate = @ExpectedDate,
           ModifiedOn   = GETDATE()
    WHERE  id = @InvoiceId;

    SELECT @@ROWCOUNT AS UpdatedRows;
END
GO
