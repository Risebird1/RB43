IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetClientWalletDashboard'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientWalletDashboard
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetClientWalletDashboard
    @ClientId     INT,
    @Month        INT,
    @Year         INT,
    @DetailsScope BIT = 0   -- 0 = month-wise details, 1 = all data
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @BillingType INT;
    DECLARE @StartDate   DATETIME = DATEFROMPARTS(@Year, @Month, 1);
    DECLARE @EndDate     DATETIME = DATEADD(MONTH, 1, @StartDate);

    SELECT @BillingType = ISNULL(BillingType, 3)
    FROM Client
    WHERE id = @ClientId;

    -- Result set 1: overall summary (always all-time for cards)
    SELECT
        c.id                                              AS ClientId,
        c.Name                                            AS ClientName,
        ISNULL(c.BillingType, 3)                          AS BillingType,
        ISNULL(c.BillingModel, 0)                         AS BillingModel,
        ISNULL(cash.CashCreditBalance, 0)                 AS CashCreditBalance,
        ISNULL(cash.CashCreditAdded, 0)                   AS CashCreditAdded,
        ISNULL(cash.CashDebitConsumed, 0)                 AS CashDebitConsumed,
        ISNULL(cash.CashCreditBalance, 0)                 AS CashRemaining,
        ISNULL(po.POCreditLimit, 0)                       AS POCreditLimit,
        ISNULL(po.POCashReceived, 0)                      AS POCashReceived,
        ISNULL(po.POCreditLimit, 0) - ISNULL(po.POCashReceived, 0) AS POCashRemaining,
        ISNULL(deb.POConsumed, 0)                         AS POConsumed,
        ISNULL(po.POCreditLimit, 0) - ISNULL(deb.POConsumed, 0) AS PORemaining,
        ISNULL(monthly.MonthCredit, 0)                    AS MonthCredit,
        ISNULL(monthly.MonthDebit, 0)                     AS MonthDebit,
        ISNULL(overall.OverallCredit, 0)                  AS OverallCredit,
        ISNULL(overall.OverallDebit, 0)                   AS OverallDebit
    FROM Client c
    OUTER APPLY (
        SELECT
            SUM(CASE WHEN ISNULL(w.TransactionType, '') IN ('Manual', 'CashCredit') THEN ISNULL(w.Credit, 0) ELSE 0 END) AS CashCreditAdded,
            SUM(ISNULL(w.Debit, 0)) AS CashDebitConsumed,
            SUM(CASE WHEN ISNULL(w.TransactionType, '') IN ('Manual', 'CashCredit') THEN ISNULL(w.Credit, 0) ELSE 0 END)
            - SUM(ISNULL(w.Debit, 0)) AS CashCreditBalance
        FROM Wallet w
        WHERE w.ClientId = @ClientId
          AND (
                (@BillingType = 3 AND w.BillingType = 3)
             OR (@BillingType = 1 AND w.BillingType = 1)
          )
    ) cash
    OUTER APPLY (
        SELECT
            SUM(ISNULL(cp.POCreditAmount, 0))     AS POCreditLimit,
            SUM(ISNULL(cp.CashCreditReceived, 0)) AS POCashReceived
        FROM ClientPO cp
        WHERE cp.ClientId = @ClientId
          AND (
                (@BillingType = 3 AND cp.BillingType = 3)
             OR (@BillingType = 1 AND cp.BillingType = 1)
          )
    ) po
    OUTER APPLY (
        SELECT SUM(ISNULL(w.Debit, 0)) AS POConsumed
        FROM Wallet w
        WHERE w.ClientId = @ClientId
          AND (
                (@BillingType = 3 AND w.BillingType = 3)
             OR (@BillingType = 1 AND w.BillingType = 1)
          )
    ) deb
    OUTER APPLY (
        SELECT
            SUM(ISNULL(w.Credit, 0)) AS MonthCredit,
            SUM(ISNULL(w.Debit, 0))  AS MonthDebit
        FROM Wallet w
        LEFT JOIN InterviewMeetings im ON im.id = w.InterviewId
        WHERE w.ClientId = @ClientId
          AND (
                (@BillingType = 3 AND w.BillingType = 3)
             OR (@BillingType = 1 AND w.BillingType = 1)
          )
          AND (
                (w.Credit > 0 AND w.CreatedOn >= @StartDate AND w.CreatedOn < @EndDate)
             OR (w.Debit > 0 AND w.InterviewId > 0 AND im.interviewDate >= @StartDate AND im.interviewDate < @EndDate)
             OR (w.Debit > 0 AND ISNULL(w.InterviewId, 0) = 0 AND w.CreatedOn >= @StartDate AND w.CreatedOn < @EndDate)
          )
    ) monthly
    OUTER APPLY (
        SELECT
            SUM(ISNULL(w.Credit, 0)) AS OverallCredit,
            SUM(ISNULL(w.Debit, 0))  AS OverallDebit
        FROM Wallet w
        WHERE w.ClientId = @ClientId
          AND (
                (@BillingType = 3 AND w.BillingType = 3)
             OR (@BillingType = 1 AND w.BillingType = 1)
          )
    ) overall
    WHERE c.id = @ClientId;

    -- Result set 2: per billing owner details (scoped by month or all)
    IF @BillingType = 1
    BEGIN
        SELECT
            ul.id AS BillingOwnerId,
            RTRIM(LTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))) AS BillingOwnerName,
            ul.EmailId AS BillingOwnerEmail,
            ISNULL(po.POCredit, 0) AS POCredit,
            ISNULL(deb.Consumed, 0) AS Consumed,
            ISNULL(po.POCredit, 0) - ISNULL(deb.Consumed, 0) AS Remaining,
            ISNULL(cash.CashBalance, 0) AS CashBalance
        FROM (
            SELECT DISTINCT IRF.BillingOwner
            FROM IRF
            WHERE IRF.ClientId = @ClientId
              AND IRF.BillingOwner IS NOT NULL
        ) owners
        INNER JOIN UserList ul ON ul.id = owners.BillingOwner
        OUTER APPLY (
            SELECT SUM(ISNULL(cp.POCreditAmount, 0)) AS POCredit
            FROM ClientPO cp
            WHERE cp.ClientId = @ClientId
              AND cp.BillingOwnerId = ul.id
              AND cp.BillingType = 1
              AND (@DetailsScope = 1 OR (cp.POMonth = @Month AND cp.POYear = @Year))
        ) po
        OUTER APPLY (
            SELECT SUM(ISNULL(w.Debit, 0)) AS Consumed
            FROM Wallet w
            LEFT JOIN InterviewMeetings im ON im.id = w.InterviewId
            WHERE w.ClientId = @ClientId
              AND w.BillingOwnerId = ul.id
              AND w.BillingType = 1
              AND (
                    @DetailsScope = 1
                 OR (w.Debit > 0 AND w.InterviewId > 0 AND im.interviewDate >= @StartDate AND im.interviewDate < @EndDate)
                 OR (w.Debit > 0 AND ISNULL(w.InterviewId, 0) = 0 AND w.CreatedOn >= @StartDate AND w.CreatedOn < @EndDate)
              )
        ) deb
        OUTER APPLY (
            SELECT
                CASE WHEN @DetailsScope = 1 THEN
                    SUM(ISNULL(w.Credit, 0)) - SUM(ISNULL(w.Debit, 0))
                ELSE
                    SUM(CASE WHEN w.Credit > 0 AND w.CreatedOn >= @StartDate AND w.CreatedOn < @EndDate THEN ISNULL(w.Credit, 0) ELSE 0 END)
                  - SUM(CASE
                        WHEN w.Debit > 0 AND w.InterviewId > 0 AND im.interviewDate >= @StartDate AND im.interviewDate < @EndDate THEN ISNULL(w.Debit, 0)
                        WHEN w.Debit > 0 AND ISNULL(w.InterviewId, 0) = 0 AND w.CreatedOn >= @StartDate AND w.CreatedOn < @EndDate THEN ISNULL(w.Debit, 0)
                        ELSE 0 END)
                END AS CashBalance
            FROM Wallet w
            LEFT JOIN InterviewMeetings im ON im.id = w.InterviewId
            WHERE w.ClientId = @ClientId
              AND w.BillingOwnerId = ul.id
              AND w.BillingType = 1
              AND ISNULL(w.TransactionType, '') IN ('Manual', 'CashCredit')
        ) cash
        ORDER BY ul.FirstName, ul.LastName;
    END
    ELSE
    BEGIN
        SELECT
            CAST(NULL AS INT)          AS BillingOwnerId,
            CAST(NULL AS VARCHAR(200)) AS BillingOwnerName,
            CAST(NULL AS VARCHAR(200)) AS BillingOwnerEmail,
            CAST(0 AS DECIMAL(18,2))   AS POCredit,
            CAST(0 AS DECIMAL(18,2))   AS Consumed,
            CAST(0 AS DECIMAL(18,2))   AS Remaining,
            CAST(0 AS DECIMAL(18,2))   AS CashBalance
        WHERE 1 = 0;
    END

    -- Result set 3: PO records (month-wise or all)
    SELECT
        cp.id,
        cp.ClientId,
        cp.BillingType,
        cp.BillingOwnerId,
        cp.BillingModel,
        cp.PONumber,
        cp.POCreditAmount,
        cp.CashCreditReceived,
        cp.POMonth,
        cp.POYear,
        cp.PODocumentUrl,
        cp.Status,
        cp.Remarks,
        cp.CreatedBy,
        cp.CreatedOn,
        cp.ModifiedOn,
        ISNULL(ul.EmailId, '') AS BillingOwnerEmail,
        RTRIM(LTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))) AS BillingOwnerName
    FROM ClientPO cp
    LEFT JOIN UserList ul ON ul.id = cp.BillingOwnerId
    WHERE cp.ClientId = @ClientId
      AND (@DetailsScope = 1 OR (cp.POMonth = @Month AND cp.POYear = @Year))
    ORDER BY cp.POYear DESC, cp.POMonth DESC, cp.CreatedOn DESC;
END
GO
