IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRevenueDetailsByDate'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRevenueDetailsByDate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Revenue summary grouped by client for a date range.
--
-- BaseAmount is computed per interview using the correct rate source:
--   InterviewerBillingType = 1 (Client)       : AIPerInterviewRate / PerInterviewRate
--   InterviewerBillingType = 2 (Skill Channel) : SkillChannelBillingRate per IRF, fallback to PerInterviewRate
--   InterviewerBillingType = 3 (Interviewer)   : InterviewerBillingRate per interviewer per IRF
--
-- Special case - Client 177 (Turing):
--   MeetingStatus IN (1, 3, 9) are also included at 30% of the normal rate.
--
-- Performance: qualifying interviews are filtered once into #Interviews, and every
-- client-level lookup (PO/Invoice/Generated/Paid/BillingOwner count) is computed once
-- per client instead of as a correlated OUTER APPLY per interview row (the old BOC
-- apply rescanned InterviewMeetings for every row, which made FNRevenue time out).
CREATE PROCEDURE usp_GetRevenueDetailsByDate
    @StartDate  DATETIME,
    @EndDate    DATETIME,
    @FromYear   INT,
    @FromMonth  INT,
    @ToYear     INT,
    @ToMonth    INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromKey INT = @FromYear * 100 + @FromMonth;
    DECLARE @ToKey   INT = @ToYear   * 100 + @ToMonth;

    -- Step 1: qualifying interviews for the period
    SELECT
        IRF.ClientId,
        IRF.id                         AS IRFRowId,
        IRF.BillingOwner,
        ISNULL(IRF.IsAIInterview, 0)   AS IsAIInterview,
        im.interviewer,
        ic.MeetingStatus,
        CAST(0 AS INT)                 AS InterviewerUserId
    INTO #Interviews
    FROM InterviewMeetings im
    JOIN IRF               ON IRF.IRFId       = im.IRFId
    JOIN InterviewComments ic ON ic.InterviewId = im.id
    WHERE im.interviewDate >= @StartDate
      AND im.interviewDate <  @EndDate
      AND (
            ic.MeetingStatus IN (4, 6, 7, 8)
            OR (IRF.ClientId = 177 AND ic.MeetingStatus IN (1, 3, 9))
          )
      AND ISNULL(im.RedoInterview, 0) = 0;

    -- Step 2: interviewer email -> UserId, one lookup per distinct email
    UPDATE i
    SET InterviewerUserId = ISNULL(u.UserId, 0)
    FROM #Interviews i
    JOIN (
        SELECT e.interviewer, x.UserId
        FROM (SELECT DISTINCT interviewer FROM #Interviews WHERE interviewer IS NOT NULL) e
        OUTER APPLY (
            SELECT TOP 1 ul_int.id AS UserId
            FROM UserList ul_int
            WHERE ul_int.EmailId = e.interviewer
        ) x
    ) u ON u.interviewer = i.interviewer;

    -- Step 3: Skill Channel rate (one per IRF, TOP 1 guards against duplicates)
    SELECT k.IRFRowId, s.BillingRate
    INTO #SkillRates
    FROM (SELECT DISTINCT IRFRowId FROM #Interviews) k
    CROSS APPLY (
        SELECT TOP 1 BillingRate
        FROM SkillChannelBillingRate
        WHERE IRFId = k.IRFRowId
    ) s;

    -- Step 4: Interviewer rate (one per IRF + user)
    SELECT k.IRFRowId, k.InterviewerUserId, r.BillingRate
    INTO #InterviewerRates
    FROM (SELECT DISTINCT IRFRowId, InterviewerUserId FROM #Interviews) k
    CROSS APPLY (
        SELECT TOP 1 BillingRate
        FROM InterviewerBillingRate
        WHERE IRFId  = k.IRFRowId
          AND Userid = k.InterviewerUserId
    ) r;

    -- Step 5: per-client aggregation of interviews
    SELECT
        i.ClientId,
        COUNT(*)                                                    AS TotalCount,
        SUM(CASE WHEN i.IsAIInterview = 0 THEN 1 ELSE 0 END)        AS NonAIInterviewCount,
        SUM(CASE WHEN i.IsAIInterview = 1 THEN 1 ELSE 0 END)        AS AIInterviewCount,
        COUNT(DISTINCT i.BillingOwner)                              AS TotalBillingOwnerCount,
        -- BaseAmount: sum of the correct rate per interview, with Turing 30% multiplier for status 1/3/9
        SUM(
            CASE
                WHEN ISNULL(cl.InterviewerBillingType, 1) = 2
                    THEN CAST(ISNULL(scbr.BillingRate, cl.PerInterviewRate) AS DECIMAL(18,2))
                WHEN ISNULL(cl.InterviewerBillingType, 1) = 3
                    THEN CAST(ISNULL(ibr.BillingRate, 0) AS DECIMAL(18,2))
                ELSE
                    CASE WHEN i.IsAIInterview = 1
                         THEN CAST(ISNULL(cl.AIPerInterviewRate, 0) AS DECIMAL(18,2))
                         ELSE CAST(ISNULL(cl.PerInterviewRate,   0) AS DECIMAL(18,2))
                    END
            END
            * CASE WHEN i.ClientId = 177 AND i.MeetingStatus IN (1, 3, 9)
                   THEN CAST(0.30 AS DECIMAL(5,2))
                   ELSE CAST(1.00 AS DECIMAL(5,2))
              END
        )                                                           AS BaseAmount
    INTO #ClientAgg
    FROM #Interviews i
    JOIN Client cl                  ON cl.id = i.ClientId
    LEFT JOIN #SkillRates scbr      ON scbr.IRFRowId = i.IRFRowId
    LEFT JOIN #InterviewerRates ibr ON ibr.IRFRowId  = i.IRFRowId
                                   AND ibr.InterviewerUserId = i.InterviewerUserId
    GROUP BY i.ClientId;

    -- Step 6: client-level revenue / invoice figures, computed once per client
    SELECT
        cl.id,
        cl.Name,
        ca.TotalCount,
        ca.NonAIInterviewCount,
        ca.AIInterviewCount,
        cl.PerInterviewRate,
        ISNULL(cl.AIPerInterviewRate, 0)        AS AIPerInterviewRate,
        cl.BillingType,
        ISNULL(cl.InterviewerBillingType, 1)    AS InterviewerBillingType,
        ISNULL(cl.BillingCurrency, 'INR')       AS BillingCurrency,
        rv.POPendingAmount,
        rv.InvoiceAmount,
        cl.GSTApplicable,
        cl.PaymentTerm,
        ISNULL(g.GenInvoiceAmount, 0)           AS GeneratedInvoiceAmount,
        ISNULL(g.GenInvoiceCount,  0)           AS GeneratedInvoiceCount,
        ISNULL(ca.TotalBillingOwnerCount, 1)    AS TotalBillingOwnerCount,
        ISNULL(g.PaidInvoiceAmount, 0)          AS ReceivedAmount,
        ca.BaseAmount
    FROM #ClientAgg ca
    JOIN Client cl ON cl.id = ca.ClientId
    LEFT JOIN (
        SELECT Clientid,
               SUM(POPendingAmount) AS POPendingAmount,
               SUM(InvoiceAmount)   AS InvoiceAmount
        FROM Revenuedetails
        WHERE (Years * 100 + Months) BETWEEN @FromKey AND @ToKey
          AND Clientid IN (SELECT ClientId FROM #ClientAgg)
        GROUP BY Clientid
    ) rv ON rv.Clientid = ca.ClientId
    LEFT JOIN (
        SELECT ClientId,
               SUM(CASE WHEN Status > 0 THEN InvoiceValue END) AS GenInvoiceAmount,
               SUM(CASE WHEN Status > 0 THEN 1 ELSE 0 END)     AS GenInvoiceCount,
               SUM(CASE WHEN Status = 5 THEN InvoiceValue END) AS PaidInvoiceAmount
        FROM GeneratedInvoices
        WHERE (InvoiceYear * 100 + InvoiceMonth) BETWEEN @FromKey AND @ToKey
          AND ClientId IN (SELECT ClientId FROM #ClientAgg)
        GROUP BY ClientId
    ) g ON g.ClientId = ca.ClientId;

    DROP TABLE #Interviews;
    DROP TABLE #SkillRates;
    DROP TABLE #InterviewerRates;
    DROP TABLE #ClientAgg;
END
GO
