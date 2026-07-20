IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPaidInvoices'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPaidInvoices
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Fetches all paid invoices (Status = 5).
-- Pass 0 to bypass a date bound.
--   @FromYear / @FromMonth : 0 = no lower bound
--   @ToYear   / @ToMonth   : 0 = no upper bound
CREATE PROCEDURE usp_GetPaidInvoices
    @FromYear  INT = 0,
    @FromMonth INT = 0,
    @ToYear    INT = 0,
    @ToMonth   INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        gi.id,
        gi.ClientId,
        ISNULL(c.Name, '')                                    AS ClientName,
        ISNULL(c.BillingModel, 0)                             AS BillingModel,
        gi.BillingOwnerId                                     AS BillingOwner,
        ISNULL(NULLIF(gi.BillingOwnerEmail, ''), ul.EmailId)  AS BillingOwnerEmail,
        gi.InvoiceNumber,
        gi.IRN,
        gi.InvoiceValue                                       AS GrossAmount,
        gi.Status,
        gi.CreatedBy                                          AS Owner,
        gi.LatestRemark,
        gi.UTRTrxn,
        ISNULL(c.BillingType, 0)                              AS BillingType,
        gi.InvoiceMonth,
        gi.InvoiceYear,
        gi.CreatedOn,
        gi.ModifiedOn,
        w.PaymentDate
    FROM GeneratedInvoices gi
    LEFT JOIN Client   c  ON c.id  = gi.ClientId
    LEFT JOIN UserList ul ON ul.id = gi.BillingOwnerId
    OUTER APPLY (
        SELECT TOP 1 w.TransactionDate AS PaymentDate
        FROM Wallet w
        WHERE w.InvoiceId = gi.id
          AND w.TransactionType = 'CashCredit'
        ORDER BY w.id DESC
    ) w
    WHERE gi.Status = 5
      AND (@FromYear  = 0 OR (gi.InvoiceYear * 100 + gi.InvoiceMonth) >= (@FromYear * 100 + @FromMonth))
      AND (@ToYear    = 0 OR (gi.InvoiceYear * 100 + gi.InvoiceMonth) <= (@ToYear   * 100 + @ToMonth))
    ORDER BY gi.InvoiceYear DESC, gi.InvoiceMonth DESC, c.Name, gi.id
END
GO
