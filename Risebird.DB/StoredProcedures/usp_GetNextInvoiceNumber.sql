IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetNextInvoiceNumber'))
    DROP PROCEDURE dbo.usp_GetNextInvoiceNumber
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns the next sequential invoice number in format RB/FIN/{FY}/{SEQ}
-- Fiscal year is April-March. e.g. May-2026 => FY 26-27
CREATE PROCEDURE usp_GetNextInvoiceNumber
    @Month INT,
    @Year  INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FYStart  INT
    DECLARE @FYEnd    INT
    DECLARE @FYLabel  VARCHAR(10)
    DECLARE @FYPrefix VARCHAR(20)
    DECLARE @SeqNo    INT

    IF @Month >= 4
    BEGIN
        SET @FYStart = @Year % 100
        SET @FYEnd   = (@Year + 1) % 100
    END
    ELSE
    BEGIN
        SET @FYStart = (@Year - 1) % 100
        SET @FYEnd   = @Year % 100
    END

    SET @FYLabel  = RIGHT('0' + CAST(@FYStart AS VARCHAR(2)), 2) + '-' + RIGHT('0' + CAST(@FYEnd AS VARCHAR(2)), 2)
    SET @FYPrefix = 'RB/FIN/' + @FYLabel + '/'

    SELECT @SeqNo = ISNULL(MAX(CAST(SUBSTRING(InvoiceNumber, LEN(@FYPrefix) + 1, 10) AS INT)), 0) + 1
    FROM GeneratedInvoices
    WHERE InvoiceNumber LIKE @FYPrefix + '%'

    SELECT @FYPrefix + RIGHT('0000' + CAST(@SeqNo AS VARCHAR(4)), 4) AS InvoiceNumber
END
GO
