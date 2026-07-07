IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetEmailLookupData'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEmailLookupData
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetEmailLookupData]
    @EmailId NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    -- Result Set 1: Basic user info
    SELECT
        ul.id                               AS UserId,
        ul.FirstName,
        ul.LastName,
        ul.EmailId,
        CAST(ul.MobileNumber AS NVARCHAR(20)) AS MobileNumber,
        ISNULL(ul.IsActive, 0)              AS IsActive,
        ISNULL(p.Name, 'Unknown')           AS ProfileType
    FROM UserList ul
    LEFT JOIN Profile p ON p.id = ul.idProfile
    WHERE ul.EmailId = @EmailId;

    -- Result Set 2: Interviewer profile (empty if not an interviewer)
    SELECT
        ipr.Id,
        ISNULL(ipr.IsApproved, 0)           AS IsApproved,
        ISNULL(ipr.CurrentRole, '')          AS CurrentRole,
        ISNULL(ipr.Experience, 0)            AS Experience,
        ISNULL(ipr.City, '')                 AS City,
        ISNULL(ipr.Country, '')              AS Country,
        ISNULL(ipr.CompanyName, '')          AS CompanyName,
        ISNULL(ipr.IsLeadInterviewer, 0)     AS IsLeadInterviewer,
        ISNULL(ipr.NickName, '')             AS NickName,
        ISNULL(ipr.IsHoldPayment, 0)         AS IsHoldPayment,
        ISNULL(ipr.HoldPaymentReason, '')    AS HoldPaymentReason,
        ISNULL(ul.IsInterviewerAccess, 0)    AS IsInterviewerAccess,
        ISNULL(ipr.IsDeleted, 0)             AS IsDeleted,
        ISNULL(tic.PAN, '')                      AS PAN,
        ISNULL(tic.AccountName, '')              AS AccountName,
        ISNULL(tic.AccountNumber, '')            AS AccountNumber,
        ISNULL(tic.IFSCCode, '')                 AS IFSCCode,
        ISNULL(tic.UPIId, '')                    AS UPIId,
        ISNULL(tic.IsBankVerified, 0)            AS IsBankVerified,
        tic.VerifiedOn                           AS BankVerifiedOn,
        ISNULL(tic.VerifiedAccountNumber, '')    AS VerifiedAccountNumber,
        ISNULL(tic.VerifiedAccountName, '')      AS VerifiedAccountName,
        ISNULL(tic.NameAtBank, '')               AS NameAtBank,
        ISNULL(tic.IsPanVerified, 0)             AS IsPanVerified,
        tic.PanVerifiedOn                        AS PanVerifiedOn,
        ISNULL(tic.VerifiedPAN, '')              AS VerifiedPAN,
        ISNULL(tic.NameAtPan, '')                AS NameAtPan,
        ISNULL(
            STUFF((
                SELECT DISTINCT ', ' + ski.Name
                FROM TransInterviewerSkill tis
                INNER JOIN Skill ski ON tis.SkillId = ski.id
                WHERE tis.InterviewerProfileId = ipr.Id
                FOR XML PATH('')
            ), 1, 2, ''),
        '') AS PrimarySkill
    FROM InterviewerProfile ipr
    INNER JOIN UserList ul ON ul.id = ipr.UserId
    LEFT JOIN TransInterviewerAccDetails tic ON tic.InterviewerProfileId = ipr.Id
    WHERE ul.EmailId = @EmailId;

    -- Result Set 3: Client profile + wallet summary + user counts (empty if not a client)
    SELECT
        c.id                                                                   AS ClientId,
        ISNULL(c.Name, '')                                                     AS ClientName,
        ISNULL(c.BillingType, 3)                                               AS BillingType,
        ISNULL(c.BillingModel, 0)                                              AS BillingModel,
        ISNULL(c.GSTApplicable, 0)                                             AS GSTApplicable,
        ISNULL(c.GST, '')                                                      AS GST,
        ISNULL(c.PerInterviewRate, 0)                                          AS PerInterviewRate,
        ISNULL(c.AIPerInterviewRate, 0)                                        AS AIPerInterviewRate,
        ISNULL(c.PaymentTerm, 0)                                               AS PaymentTerm,
        ISNULL(c.ClientType, 0)                                                AS ClientType,
        ISNULL(c.CountryType, 0)                                               AS CountryType,
        ISNULL(c.BillingCurrency, 'INR')                                       AS BillingCurrency,
        -- Active users under this client
        (SELECT COUNT(*)
         FROM ClientUser cu2
         INNER JOIN UserList ul2 ON ul2.id = cu2.UserId
         WHERE cu2.ClientId = c.id AND ISNULL(ul2.IsActive, 0) = 1)           AS ActiveUserCount,
        -- Total users under this client
        (SELECT COUNT(*) FROM ClientUser cu2 WHERE cu2.ClientId = c.id)        AS TotalUserCount,
        -- Wallet: overall credits and debits (all time)
        ISNULL((SELECT SUM(ISNULL(w.Credit, 0)) FROM Wallet w WHERE w.ClientId = c.id), 0)  AS TotalCredits,
        ISNULL((SELECT SUM(ISNULL(w.Debit, 0))  FROM Wallet w WHERE w.ClientId = c.id), 0)  AS TotalDebits,
        -- Cash credits (Manual / CashCredit transactions only)
        ISNULL((SELECT SUM(CASE WHEN ISNULL(w.TransactionType,'') IN ('Manual','CashCredit')
                                THEN ISNULL(w.Credit, 0) ELSE 0 END)
                FROM Wallet w WHERE w.ClientId = c.id), 0)                     AS CashCreditAdded,
        -- PO summary
        ISNULL((SELECT SUM(ISNULL(cp.POCreditAmount, 0))     FROM ClientPO cp WHERE cp.ClientId = c.id), 0) AS TotalPOAmount,
        ISNULL((SELECT SUM(ISNULL(cp.CashCreditReceived, 0)) FROM ClientPO cp WHERE cp.ClientId = c.id), 0) AS TotalPOReceived
    FROM Client c
    INNER JOIN ClientUser cu ON cu.ClientId = c.id
    INNER JOIN UserList ul   ON ul.id = cu.UserId
    WHERE ul.EmailId = @EmailId;

    -- Result Set 4: Recent POs for this client (last 10)
    SELECT TOP 10
        cp.id,
        ISNULL(cp.PONumber, '')                AS PONumber,
        ISNULL(cp.POCreditAmount, 0)           AS POCreditAmount,
        ISNULL(cp.CashCreditReceived, 0)       AS CashCreditReceived,
        ISNULL(cp.POMonth, 0)                  AS POMonth,
        ISNULL(cp.POYear, 0)                   AS POYear,
        ISNULL(cp.Status, 0)                   AS Status,
        ISNULL(cp.Remarks, '')                 AS Remarks,
        cp.CreatedOn
    FROM ClientPO cp
    INNER JOIN ClientUser cu ON cu.ClientId = cp.ClientId
    INNER JOIN UserList ul   ON ul.id = cu.UserId
    WHERE ul.EmailId = @EmailId
    ORDER BY cp.POYear DESC, cp.POMonth DESC, cp.CreatedOn DESC;

    -- Result Set 5: Interview stats as Candidate
    -- confirmationStatus = 1 → active/scheduled, = 2 → cancelled
    -- confirmationStatus = 1 → active/scheduled, = 2 → cancelled
    SELECT
        COUNT(DISTINCT im.id)                                                                                            AS TotalCount,
        COUNT(DISTINCT CASE WHEN ic.MeetingStatus IN (4,6,7,8) AND im.confirmationStatus != 2 THEN im.id END)           AS TotalCompleted,
        COUNT(DISTINCT CASE WHEN im.confirmationStatus = 2   OR ic.MeetingStatus IN (1,3,5,9) THEN im.id END)           AS TotalCancelled,
        COUNT(DISTINCT CASE WHEN im.confirmationStatus != 2  AND (ic.MeetingStatus = 2 OR ic.MeetingStatus IS NULL)
                              THEN im.id END)                                                                            AS TotalScheduled
    FROM InterviewMeetings im
    LEFT JOIN InterviewComments ic ON ic.InterviewId = im.id
    WHERE im.candidateEmail = @EmailId
      AND ISNULL(im.IsDeleted, 0) = 0;

    -- Result Set 6: Interview stats as Interviewer
    SELECT
        COUNT(DISTINCT im.id)                                                                                            AS TotalCount,
        COUNT(DISTINCT CASE WHEN ic.MeetingStatus IN (4,6,7,8) AND im.confirmationStatus != 2 THEN im.id END)           AS TotalCompleted,
        COUNT(DISTINCT CASE WHEN im.confirmationStatus = 2   OR ic.MeetingStatus IN (1,3,5,9) THEN im.id END)           AS TotalCancelled,
        COUNT(DISTINCT CASE WHEN im.confirmationStatus != 2  AND (ic.MeetingStatus = 2 OR ic.MeetingStatus IS NULL)
                              THEN im.id END)                                                                            AS TotalScheduled
    FROM InterviewMeetings im
    LEFT JOIN InterviewComments ic ON ic.InterviewId = im.id
    WHERE im.interviewer = @EmailId
      AND ISNULL(im.IsDeleted, 0) = 0;

    -- Result Set 7: Payment stats from InterviewPoints (actual earned amounts)
    DECLARE @UserId INT;
    SELECT @UserId = id FROM UserList WHERE EmailId = @EmailId;

    SELECT
        ISNULL(SUM(
            CASE WHEN ip.Points >= 0
                 THEN ip.Points - (ip.Points * ISNULL(ip.PenaltyAmount, 0) / 100)
                 ELSE ip.Points END
        ), 0) AS TotalPaymentAmount,
        ISNULL(SUM(
            CASE WHEN ip.Status = 'Paid'
                 THEN ip.Points - (ip.Points * ISNULL(ip.PenaltyAmount, 0) / 100)
                 ELSE 0 END
        ), 0) AS TotalPaidAmount,
        ISNULL(SUM(
            CASE WHEN ip.Status = 'Pending' OR ip.Status = 'Hold'
                 THEN ip.Points - (ip.Points * ISNULL(ip.PenaltyAmount, 0) / 100)
                 ELSE 0 END
        ), 0) AS TotalPendingPayment
    FROM InterviewPoints ip
    WHERE ip.userid = @UserId
      AND ip.Points > 0;

    -- Result Set 8: Recent meetings as Candidate (last 20)
    SELECT TOP 20
        im.id                AS Id,
        im.IRFId,
        im.interviewDate     AS InterviewDate,
        im.interviewSlot     AS InterviewSlot,
        CASE
            WHEN im.confirmationStatus = 2    THEN 'Cancelled'
            WHEN ic.MeetingStatus IN (4,6,7,8) THEN 'Completed'
            WHEN ic.MeetingStatus IN (1,3,5,9) THEN 'No Show / Dropped'
            WHEN ic.MeetingStatus = 2          THEN 'Scheduled'
            ELSE 'Scheduled'
        END                  AS Status,
        im.candidateEmail    AS CandidateEmail,
        im.candidateName     AS CandidateName,
        im.interviewer       AS Interviewer
    FROM InterviewMeetings im
    LEFT JOIN InterviewComments ic ON ic.InterviewId = im.id
    WHERE im.candidateEmail = @EmailId
      AND ISNULL(im.IsDeleted, 0) = 0
    ORDER BY im.interviewDate DESC;

    -- Result Set 9: Recent meetings as Interviewer (last 20)
    SELECT TOP 20
        im.id                AS Id,
        im.IRFId,
        im.interviewDate     AS InterviewDate,
        im.interviewSlot     AS InterviewSlot,
        CASE
            WHEN im.confirmationStatus = 2     THEN 'Cancelled'
            WHEN ic.MeetingStatus IN (4,6,7,8) THEN 'Completed'
            WHEN ic.MeetingStatus IN (1,3,5,9) THEN 'No Show / Dropped'
            WHEN ic.MeetingStatus = 2          THEN 'Scheduled'
            ELSE 'Scheduled'
        END                  AS Status,
        im.candidateEmail    AS CandidateEmail,
        im.candidateName     AS CandidateName,
        im.interviewer       AS Interviewer
    FROM InterviewMeetings im
    LEFT JOIN InterviewComments ic ON ic.InterviewId = im.id
    WHERE im.interviewer = @EmailId
      AND ISNULL(im.IsDeleted, 0) = 0
    ORDER BY im.interviewDate DESC;

    -- Result Set 10: Recent payouts from PaymentHistory (last 20)
    SELECT TOP 20
        ph.id               AS Id,
        ph.PaymentDate,
        ISNULL(ph.Amount, 0)      AS Amount,
        ISNULL(ph.TotalAmount, 0) AS TotalAmount,
        ph.Status,
        ph.Comment,
        ph.PayoutStatus,
        ph.TransactionDate
    FROM PaymentHistory ph
    WHERE ph.Userid = @UserId
    ORDER BY ph.PaymentDate DESC;

    -- Result Set 11: Payment-driven quarter breakdown.
    -- Starts from PaymentHistory (all quarters with real payments appear),
    -- then LEFT JOINs TDS challan data so TDS figures are shown where available.
    -- Indian FY: Apr-Jun = Q1, Jul-Sep = Q2, Oct-Dec = Q3, Jan-Mar = Q4.
    ;WITH PhByQuarter AS (
        SELECT
            CASE WHEN MONTH(ph.PaymentDate) >= 4
                 THEN CAST(YEAR(ph.PaymentDate)   AS NVARCHAR(4)) + '-'
                      + RIGHT(CAST(YEAR(ph.PaymentDate) + 1 AS NVARCHAR(4)), 2)
                 ELSE CAST(YEAR(ph.PaymentDate)-1 AS NVARCHAR(4)) + '-'
                      + RIGHT(CAST(YEAR(ph.PaymentDate)   AS NVARCHAR(4)), 2)
            END AS FinancialYear,
            CASE WHEN MONTH(ph.PaymentDate) IN (4,5,6)    THEN 'Q1'
                 WHEN MONTH(ph.PaymentDate) IN (7,8,9)    THEN 'Q2'
                 WHEN MONTH(ph.PaymentDate) IN (10,11,12) THEN 'Q3'
                 ELSE                                          'Q4'
            END AS Quarter,
            ph.TotalAmount
        FROM PaymentHistory ph
        WHERE ph.Userid = @UserId
          AND ph.TotalAmount > 0
          AND (ph.Status = 'Done' OR ph.PayoutStatus = 'TRANSFER_SUCCESS')
          AND ph.PaymentDate >= '2026-04-01'   -- FY 2026-27 onwards
    ),
    PhAgg AS (
        SELECT FinancialYear, Quarter, SUM(TotalAmount) AS TotalAmountPaid
        FROM   PhByQuarter
        GROUP  BY FinancialYear, Quarter
    ),
    -- Aggregate all weekly challans per FY+Quarter.
    -- TdsChallan.Quarter is stored as 'Quarter 1' / 'Quarter 2' etc.
    -- Map to 'Q1'–'Q4' so it joins correctly with PhAgg.
    TdsAgg AS (
        SELECT
            -- Normalise '2026-2027' → '2026-27' to match PhAgg format
            LEFT(tc.FinancialYear, 4) + '-' + RIGHT(tc.FinancialYear, 2) AS FinancialYear,
            -- Normalise 'Quarter 1' → 'Q1' to match PhAgg format
            CASE tc.Quarter
                WHEN 'Quarter 1' THEN 'Q1'
                WHEN 'Quarter 2' THEN 'Q2'
                WHEN 'Quarter 3' THEN 'Q3'
                WHEN 'Quarter 4' THEN 'Q4'
                ELSE LEFT(tc.Quarter, 2)
            END                                           AS Quarter,
            ISNULL(SUM(tci.TdsAmount), 0)                 AS TdsAmount,
            CAST(MAX(CAST(tc.IsPaid AS INT)) AS BIT)      AS IsPaid,
            MIN(tc.DepositDate)                           AS DepositDate,
            ISNULL(MIN(tc.BSRCode), '')                   AS BSRCode,
            ISNULL(MIN(tc.ChallanSerialNumber), '')       AS ChallanSerialNumber
        FROM TdsChallanInterviewer tci
        INNER JOIN TdsChallan tc ON tc.id = tci.ChallanId
        WHERE tci.Userid = @UserId
        GROUP BY
            LEFT(tc.FinancialYear, 4) + '-' + RIGHT(tc.FinancialYear, 2),
            CASE tc.Quarter
                WHEN 'Quarter 1' THEN 'Q1'
                WHEN 'Quarter 2' THEN 'Q2'
                WHEN 'Quarter 3' THEN 'Q3'
                WHEN 'Quarter 4' THEN 'Q4'
                ELSE LEFT(tc.Quarter, 2)
            END
    )
    SELECT
        0                                   AS ChallanId,
        pa.FinancialYear,
        pa.Quarter,
        pa.TotalAmountPaid                  AS AmountPaid,
        ISNULL(td.TdsAmount, 0)             AS TdsAmount,
        ISNULL(td.IsPaid, 0)               AS IsPaid,
        td.DepositDate,
        ISNULL(td.BSRCode, '')              AS BSRCode,
        ISNULL(td.ChallanSerialNumber, '')  AS ChallanSerialNumber
    FROM      PhAgg   pa
    LEFT JOIN TdsAgg  td ON td.FinancialYear = pa.FinancialYear
                        AND td.Quarter       = pa.Quarter
    ORDER BY pa.FinancialYear DESC, pa.Quarter ASC;

    -- Result Set 12: Grand total from PaymentHistory (used in summary header card)
    SELECT ISNULL(SUM(ph.TotalAmount), 0) AS TotalHistoryPaid
    FROM   PaymentHistory ph
    WHERE  ph.Userid = @UserId
      AND  ph.TotalAmount > 0
      AND  (ph.Status = 'Done' OR ph.PayoutStatus = 'TRANSFER_SUCCESS');

END
GO
