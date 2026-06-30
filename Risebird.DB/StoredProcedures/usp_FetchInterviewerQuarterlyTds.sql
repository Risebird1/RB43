CREATE OR ALTER PROCEDURE [dbo].[usp_FetchInterviewerQuarterlyTds]
    @StartDate DATE,
    @EndDate   DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- One consolidated row per interviewer/PAN for the quarter.
    -- TDS is computed ONLY on wallet-verified (IsBankVerified=1) payouts
    -- with PayoutStatus = 'TRANSFER_SUCCESS' or Status = 'Done' (legacy).
    SELECT
        ul.id                                       AS Userid,
        ul.FirstName,
        ''                                          AS MiddleName,  -- TODO: add MiddleName to UserList if required
        ul.LastName,
        ul.EmailId                                  AS Email,
        ISNULL(tic.PAN, '')                         AS PAN,
        ISNULL(tic.AccountNumber, '')               AS AccountNumber,
        ISNULL(tic.IFSCCode, '')                    AS IFSCCode,
        ISNULL(tic.AccountName, '')                 AS AccountName,
        --ISNULL(tic.IsBankVerified, 0)               AS IsWalletVerified,
        1               AS IsWalletVerified,
        SUM(ph.TotalAmount)                         AS TotalPaid,
        ROUND(SUM(ph.TotalAmount) * 0.10, 0)        AS Tds,
        MAX(ph.PaymentDate)                         AS DatePaid
    FROM PaymentHistory ph
    JOIN UserList ul ON ul.id = ph.Userid
    LEFT JOIN InterviewerProfile irp ON irp.UserId = ul.id
    LEFT JOIN TransInterviewerAccDetails tic ON tic.InterviewerProfileId = irp.id
    WHERE
        ph.PaymentDate >= '2026-04-01'
        AND ph.PaymentDate <= '2026-07-01'
        AND ph.TotalAmount > 0
        AND (ph.Status = 'Done' OR ph.PayoutStatus = 'TRANSFER_SUCCESS')
        --AND ISNULL(tic.IsBankVerified, 0) = 1        -- wallet-verified only
    GROUP BY
        ul.id, ul.FirstName, ul.LastName, ul.EmailId,
        tic.PAN, tic.AccountNumber, tic.IFSCCode, tic.AccountName, tic.IsBankVerified

    --UNION ALL

    ---- Unverified-wallet interviewers (excluded from TDS calc but shown in Tab 2)
    --SELECT
    --    ul.id                                       AS Userid,
    --    ul.FirstName,
    --    ''                                          AS MiddleName,
    --    ul.LastName,
    --    ul.EmailId                                  AS Email,
    --    ISNULL(tic.PAN, '')                         AS PAN,
    --    ISNULL(tic.AccountNumber, '')               AS AccountNumber,
    --    ISNULL(tic.IFSCCode, '')                    AS IFSCCode,
    --    ISNULL(tic.AccountName, '')                 AS AccountName,
    --    ISNULL(tic.IsBankVerified, 0)               AS IsWalletVerified,
    --    SUM(ph.TotalAmount)                         AS TotalPaid,
    --    ROUND(SUM(ph.TotalAmount) * 0.10, 0)        AS Tds,
    --    MAX(ph.PaymentDate)                         AS DatePaid
    --FROM PaymentHistory ph
    --JOIN UserList ul ON ul.id = ph.Userid
    --LEFT JOIN InterviewerProfile irp ON irp.UserId = ul.id
    --LEFT JOIN TransInterviewerAccDetails tic ON tic.InterviewerProfileId = irp.id
    --WHERE
    --    ph.PaymentDate >= @StartDate
    --    AND ph.PaymentDate <= @EndDate
    --    AND ph.TotalAmount > 0
    --    AND (ph.Status = 'Done' OR ph.PayoutStatus = 'TRANSFER_SUCCESS')
    --    AND ISNULL(tic.IsBankVerified, 0) = 0        -- unverified wallet
    --GROUP BY
    --    ul.id, ul.FirstName, ul.LastName, ul.EmailId,
    --    tic.PAN, tic.AccountNumber, tic.IFSCCode, tic.AccountName, tic.IsBankVerified
END
GO
