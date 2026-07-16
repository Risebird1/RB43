CREATE OR ALTER PROCEDURE [dbo].[usp_FetchInterviewerQuarterlyTds]
    @StartDate DATE,
    @EndDate   DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Step 1: calculate TDS on each individual payment row first (ROUND per payment),
    -- then aggregate. This ensures rounding happens at payment level, not on the total.
    WITH PaymentTds AS (
        SELECT
            ph.Userid,
            ph.TotalAmount                              AS PaymentAmount,
            ROUND(ph.TotalAmount * 0.10, 0)             AS PaymentTds,  -- TDS rounded per payment
            ph.PaymentDate
        FROM PaymentHistory ph
        WHERE
            CAST(ph.PaymentDate AS DATE) >= @StartDate
            AND CAST(ph.PaymentDate AS DATE) <= @EndDate
            AND ph.TotalAmount > 0
            AND (ph.Status = 'Done' OR ph.PayoutStatus = 'TRANSFER_SUCCESS')
    )
    SELECT
        ul.id                                       AS Userid,
        ul.FirstName,
        ''                                          AS MiddleName,  -- TODO: add MiddleName to UserList if required
        ul.LastName,
        ul.EmailId                                  AS Email,
        ISNULL(tic.PAN, '')                         AS PAN,
        ISNULL(tic.IsPanVerified, 0)                AS IsPanVerified,
        ISNULL(tic.NameAtPan, '')                   AS NameAtPan,
        ISNULL(tic.AccountNumber, '')               AS AccountNumber,
        ISNULL(tic.IFSCCode, '')                    AS IFSCCode,
        ISNULL(tic.AccountName, '')                 AS AccountName,
        --ISNULL(tic.IsBankVerified, 0)               AS IsWalletVerified,
        1                                           AS IsWalletVerified,
        SUM(pt.PaymentAmount)                       AS TotalPaid,
        SUM(pt.PaymentTds)                          AS Tds,           -- sum of per-payment rounded TDS
        MAX(pt.PaymentDate)                         AS DatePaid
    FROM PaymentTds pt
    JOIN UserList ul ON ul.id = pt.Userid
    LEFT JOIN InterviewerProfile irp ON irp.UserId = ul.id
    LEFT JOIN TransInterviewerAccDetails tic ON tic.InterviewerProfileId = irp.id
    --AND ISNULL(tic.IsBankVerified, 0) = 1        -- wallet-verified only
    GROUP BY
        ul.id, ul.FirstName, ul.LastName, ul.EmailId,
        tic.PAN, tic.IsPanVerified, tic.NameAtPan,
        tic.AccountNumber, tic.IFSCCode, tic.AccountName, tic.IsBankVerified

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
    --    SUM(pt.PaymentAmount)                       AS TotalPaid,
    --    SUM(pt.PaymentTds)                          AS Tds,
    --    MAX(pt.PaymentDate)                         AS DatePaid
    --FROM PaymentTds pt
    --JOIN UserList ul ON ul.id = pt.Userid
    --LEFT JOIN InterviewerProfile irp ON irp.UserId = ul.id
    --LEFT JOIN TransInterviewerAccDetails tic ON tic.InterviewerProfileId = irp.id
    --WHERE ISNULL(tic.IsBankVerified, 0) = 0        -- unverified wallet
    --GROUP BY
    --    ul.id, ul.FirstName, ul.LastName, ul.EmailId,
    --    tic.PAN, tic.AccountNumber, tic.IFSCCode, tic.AccountName, tic.IsBankVerified
END
GO
