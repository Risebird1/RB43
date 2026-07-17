-- ============================================================
-- usp_GetAllClientsPOData
-- Returns two result sets for the PO Summary report page:
--   RS1: All ClientPO rows for all clients, with client meta
--        (ClientName, BillingType, BillingModel from Client table).
--   RS2: All wallet debits grouped by (ClientId, BillingOwnerId,
--        BillingType, Month, Year) — used for FIFO distribution.
-- No parameters — the caller (C#) handles any filtering in memory.
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetAllClientsPOData]
AS
BEGIN
    SET NOCOUNT ON;

    -- ── RS1 : All PO records across all clients ──────────────────────────────
    SELECT
        cp.id,
        cp.ClientId,
        c.Name                                                          AS ClientName,
        ISNULL(c.BillingType, 3)                                       AS BillingType,
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
        ISNULL(ul.EmailId, '')                                         AS BillingOwnerEmail,
        RTRIM(LTRIM(ISNULL(ul.FirstName,'') + ' ' + ISNULL(ul.LastName,'')))
                                                                        AS BillingOwnerName
    FROM ClientPO cp
    INNER JOIN Client c ON c.id = cp.ClientId
    LEFT  JOIN UserList ul ON ul.id = cp.BillingOwnerId
    ORDER BY cp.ClientId, cp.POYear, cp.POMonth, cp.CreatedOn;

    -- ── RS2 : Monthly wallet debits per client / billing-owner ───────────────
    SELECT
        w.ClientId,
        w.BillingOwnerId,
        w.BillingType,
        CASE WHEN w.InterviewId > 0
             THEN MONTH(im.interviewDate)
             ELSE MONTH(w.CreatedOn)
        END                                                             AS [Month],
        CASE WHEN w.InterviewId > 0
             THEN YEAR(im.interviewDate)
             ELSE YEAR(w.CreatedOn)
        END                                                             AS [Year],
        SUM(ISNULL(w.Debit, 0))                                        AS TotalDebit
    FROM Wallet w
    LEFT JOIN InterviewMeetings im ON im.id = w.InterviewId AND w.InterviewId > 0
    WHERE w.Debit > 0
    GROUP BY
        w.ClientId,
        w.BillingOwnerId,
        w.BillingType,
        CASE WHEN w.InterviewId > 0 THEN MONTH(im.interviewDate) ELSE MONTH(w.CreatedOn) END,
        CASE WHEN w.InterviewId > 0 THEN YEAR(im.interviewDate)  ELSE YEAR(w.CreatedOn)  END
    ORDER BY w.ClientId,
        CASE WHEN w.InterviewId > 0 THEN YEAR(im.interviewDate)  ELSE YEAR(w.CreatedOn)  END,
        CASE WHEN w.InterviewId > 0 THEN MONTH(im.interviewDate) ELSE MONTH(w.CreatedOn) END;
END
GO
