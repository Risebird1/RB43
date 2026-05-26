IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetClientCashCreditHistory'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientCashCreditHistory
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetClientCashCreditHistory
    @ClientId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        w.id,
        w.BillingType,
        w.ClientId,
        w.BillingOwnerId,
        w.CreatedBy,
        w.Credit,
        w.CreatedOn,
        w.TransactionType,
        w.TransactionDate,
        w.TransactionRefNumber,
        w.InvoicePDF,
        w.ClientPOId,
        ISNULL(cp.PONumber, '') AS PONumber,
        ISNULL(ul.EmailId, '') AS BillingOwnerEmail,
        RTRIM(LTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))) AS BillingOwnerName
    FROM Wallet w
    LEFT JOIN ClientPO cp ON cp.id = w.ClientPOId
    LEFT JOIN UserList ul ON ul.id = w.BillingOwnerId
    WHERE w.ClientId = @ClientId
      AND w.Credit > 0
      AND ISNULL(w.TransactionType, '') IN ('CashCredit', 'Manual')
    ORDER BY w.CreatedOn DESC, w.id DESC;
END
GO
