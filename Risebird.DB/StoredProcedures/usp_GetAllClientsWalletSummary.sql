-- ============================================================
-- usp_GetAllClientsWalletSummary
-- Returns two result sets for the Wallet Details page:
--   RS1: One row per client — month credit/debit, interview count,
--        BillingType from Client table.
--   RS2: Billing-owner-wise interview count, BillingOwner from IRF.
--
-- MeetingStatus rules (from InterviewComments):
--   ClientId = 177 : count statuses 1,3,4,6,7,8,9
--   All others     : count statuses 4,6,7,8  (completed only)
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetAllClientsWalletSummary] 
    @Month  INT,
    @Year   INT
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Interview counts per client for the target month ──────────────────
    ;WITH InterviewCountPerClient AS
    (
        SELECT
            irf.ClientId,
            COUNT(DISTINCT im.id) AS MonthInterviewCount
        FROM InterviewMeetings im
        INNER JOIN IRF            irf ON irf.IRFId      = im.IRFId
        LEFT  JOIN InterviewComments ic ON ic.InterviewId = im.id
        WHERE MONTH(im.interviewDate) = @Month
          AND YEAR(im.interviewDate)  = @Year
          AND (
                (irf.ClientId = 177 AND ic.MeetingStatus IN (1,3,4,6,7,8,9))
             OR (irf.ClientId <> 177 AND ic.MeetingStatus IN (4,6,7,8))
              )
         AND ISNULL(im.RedoInterview,0)=0
        GROUP BY irf.ClientId
    )

    -- ── RS1: Client-wise summary ───────────────────────────────────────────
    SELECT
        c.id                                                        AS ClientId,
        c.Name                                                      AS ClientName,
        c.BillingModel,
        c.PerInterviewRate,
        c.AIPerInterviewRate,
        ISNULL(c.BillingType, 3)                                   AS BillingType,
        ISNULL(c.BillingCurrency, 'INR')                           AS BillingCurrency,
        ic.MonthInterviewCount,
        ISNULL(SUM(CASE WHEN MONTH(w.TransactionDate) = @Month
                         AND YEAR(w.TransactionDate)  = @Year
                        THEN w.Credit ELSE 0 END), 0)              AS MonthCredit,
        ISNULL(SUM(CASE WHEN MONTH(w.TransactionDate) = @Month
                         AND YEAR(w.TransactionDate)  = @Year
                        THEN w.Debit  ELSE 0 END), 0)              AS MonthDebit,
        COUNT(CASE WHEN MONTH(w.TransactionDate) = @Month
                    AND YEAR(w.TransactionDate)  = @Year
                   THEN 1 END)                                      AS TransactionCount,
        ISNULL(SUM(w.Credit), 0) - ISNULL(SUM(w.Debit), 0)        AS OverallBalance
    FROM Client c
    INNER JOIN InterviewCountPerClient ic ON ic.ClientId = c.id
    LEFT  JOIN Wallet w ON w.ClientId = c.id
    GROUP BY
        c.id, c.Name, c.BillingModel, c.BillingType, c.BillingCurrency,
        c.PerInterviewRate, c.AIPerInterviewRate,
        ic.MonthInterviewCount
    HAVING ic.MonthInterviewCount > 0
    ORDER BY
        ic.MonthInterviewCount DESC,
        c.Name;

    -- ── RS2: Billing-owner-wise interview count + month credit/debit ─────
    -- Only relevant when BillingType = 1 (User Scope); credit/debit pulled
    -- from Wallet filtered by BillingOwnerId and month.
	;WITH WalletSummary AS
(
    SELECT
        ClientId,
        BillingOwnerId,
        SUM(Credit) AS MonthCredit,
        SUM(Debit)  AS MonthDebit
    FROM Wallet
    WHERE BillingType = 1
      AND CreatedOn >= DATEFROMPARTS(@Year,@Month,1)
      AND CreatedOn < DATEADD(MONTH,1,DATEFROMPARTS(@Year,@Month,1))
    GROUP BY ClientId, BillingOwnerId
)
    SELECT
        irf.ClientId,
        ISNULL(irf.BillingOwner, 0)                                             AS BillingOwnerId,
        ISNULL(ul.EmailId, 'Unassigned')                                        AS BillingOwnerEmail,
        ISNULL(RTRIM(LTRIM(ISNULL(ul.FirstName,'') + ' ' + ISNULL(ul.LastName,''))), 'Unassigned') AS BillingOwnerName,
        COUNT(DISTINCT im.id)                                                   AS MonthInterviewCount,
       ISNULL(ws.MonthCredit,0) AS MonthCredit,
       ISNULL(ws.MonthDebit,0) AS MonthDebit
    FROM InterviewMeetings im
    INNER JOIN IRF             irf ON irf.IRFId       = im.IRFId
    LEFT  JOIN InterviewComments ic  ON ic.InterviewId  = im.id
    LEFT  JOIN UserList         ul   ON ul.id            = irf.BillingOwner
    LEFT JOIN WalletSummary ws ON ws.ClientId = irf.ClientId   AND ws.BillingOwnerId = irf.BillingOwner
    WHERE MONTH(im.interviewDate) = @Month
      AND YEAR(im.interviewDate)  = @Year
      AND ISNULL(im.RedoInterview, 0) = 0
      AND (
            (irf.ClientId = 177 AND ic.MeetingStatus IN (1,3,4,6,7,8,9))
         OR (irf.ClientId <> 177 AND ic.MeetingStatus IN (4,6,7,8))
          )
    GROUP BY
        irf.ClientId,
        irf.BillingOwner,
        ul.EmailId,
        ul.FirstName,
        ul.LastName,
		ws.MonthCredit,
        ws.MonthDebit
    ORDER BY
        irf.ClientId,
        MonthInterviewCount DESC;
END
GO

