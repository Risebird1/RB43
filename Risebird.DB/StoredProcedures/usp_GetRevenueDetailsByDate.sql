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
-- Rate resolution (InterviewerBillingType 1 & 2 — effective-date aware):
--
--   InterviewerBillingType = 1 (Client):
--       Per-interview rate = ClientBillingRateHistory.PerSuccessRate
--           WHERE EffectiveFrom <= im.interviewDate (most recent match)
--       Fallback: Client.PerInterviewRate / AIPerInterviewRate
--
--   InterviewerBillingType = 2 (Skill Channel):
--       Per-interview rate = SkillChannelBillingRate.BillingRate
--           WHERE IRFId matches AND EffectiveFrom <= im.interviewDate (most recent match)
--       Fallback: ClientBillingRateHistory then Client.PerInterviewRate
--
--   InterviewerBillingType = 3 (Interviewer):
--       Per-interview rate = InterviewerBillingRate.BillingRate (no effective-date logic)
--
-- CurrentRate (display column):
--   Single value per client — most recent ClientBillingRateHistory.PerSuccessRate as of today.
--   Fallback: Client.PerInterviewRate.
--   Resolved via scalar subquery on cl.id so it never breaks the GROUP BY grouping.
--
-- Special case - Client 177 (Turing):
--   MeetingStatus IN (1, 3, 9) are also included at 30% of the normal rate.
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

    SELECT
        cl.id,
        cl.Name,
        COUNT(im.id)                                                                      AS TotalCount,
        SUM(CASE WHEN ISNULL(IRF.IsAIInterview, 0) = 0 THEN 1 ELSE 0 END)               AS NonAIInterviewCount,
        SUM(CASE WHEN ISNULL(IRF.IsAIInterview, 0) = 1 THEN 1 ELSE 0 END)               AS AIInterviewCount,
        cl.PerInterviewRate,
        ISNULL(cl.AIPerInterviewRate, 0)                                                  AS AIPerInterviewRate,
        cl.BillingType,
        ISNULL(cl.InterviewerBillingType, 1)                                              AS InterviewerBillingType,
        ISNULL(cl.BillingCurrency, 'INR')                                                 AS BillingCurrency,
        Po.POPendingAmount,
        Inv.InvoiceAmount,
        cl.GSTApplicable,
        cl.PaymentTerm,
        ISNULL(GI.GenInvoiceAmount, 0)                                                    AS GeneratedInvoiceAmount,
        ISNULL(GI.GenInvoiceCount,  0)                                                    AS GeneratedInvoiceCount,
        ISNULL(BOC.TotalBillingOwnerCount, 1)                                             AS TotalBillingOwnerCount,
        ISNULL(PI.PaidInvoiceAmount, 0)                                                   AS ReceivedAmount,

        -- CurrentRate: most recent PerSuccessRate from history as of today, fallback Client.PerInterviewRate.
        -- GSTApplicable: most recent value from history as of today, fallback Client.GSTApplicable.
        -- Both use scalar subqueries keyed only to cl.id — no GROUP BY impact.
        ISNULL(
            (SELECT TOP 1 PerSuccessRate
             FROM ClientBillingRateHistory
             WHERE ClientId      = cl.id
               AND EffectiveFrom <= CAST(GETDATE() AS DATE)
             ORDER BY EffectiveFrom DESC),
            cl.PerInterviewRate
        )                                                                                  AS CurrentRate,
        ISNULL(
            (SELECT TOP 1 GSTApplicable
             FROM ClientBillingRateHistory
             WHERE ClientId      = cl.id
               AND EffectiveFrom <= CAST(GETDATE() AS DATE)
               AND GSTApplicable IS NOT NULL
             ORDER BY EffectiveFrom DESC),
            cl.GSTApplicable
        )                                                                                  AS GSTApplicable,

        -- BaseAmount: per-interview effective-date rate lookup summed across all interviews.
        -- AI interviews always use Client.AIPerInterviewRate regardless of billing type.
        -- Non-AI interviews use the PerSuccessRate effective at the interview date.
        SUM(
            CASE
                -- AI interview: always Client.AIPerInterviewRate
                WHEN ISNULL(IRF.IsAIInterview, 0) = 1
                    THEN CAST(ISNULL(cl.AIPerInterviewRate, 0) AS DECIMAL(18,2))

                -- Non-AI, InterviewerBillingType = 2: SkillChannel effective-date rate
                WHEN ISNULL(cl.InterviewerBillingType, 1) = 2
                    THEN CAST(
                            ISNULL(
                                scbr_eff.BillingRate,
                                ISNULL(cbrh_eff.PerSuccessRate, cl.PerInterviewRate)
                            ) AS DECIMAL(18,2))

                -- Non-AI, InterviewerBillingType = 3: per-interviewer rate
                WHEN ISNULL(cl.InterviewerBillingType, 1) = 3
                    THEN CAST(ISNULL(ibr.BillingRate, 0) AS DECIMAL(18,2))

                -- Non-AI, InterviewerBillingType = 1 (Client): PerSuccessRate history rate
                ELSE
                    CAST(ISNULL(cbrh_eff.PerSuccessRate, cl.PerInterviewRate) AS DECIMAL(18,2))
            END
            * CASE WHEN cl.id = 177 AND ic.MeetingStatus IN (1, 3, 9)
                   THEN CAST(0.30 AS DECIMAL(5,2))
                   ELSE CAST(1.00 AS DECIMAL(5,2))
              END
        )                                                                                  AS BaseAmount

    FROM InterviewMeetings im
    JOIN IRF               ON IRF.IRFId       = im.IRFId
    JOIN InterviewComments ic ON ic.InterviewId = im.id
    JOIN Client cl         ON IRF.ClientId    = cl.id

    -- Effective-date rate for InterviewerBillingType = 1 (Client)
    OUTER APPLY (
        SELECT TOP 1 PerSuccessRate
        FROM ClientBillingRateHistory
        WHERE ClientId     = cl.id
          AND EffectiveFrom <= CAST(im.interviewDate AS DATE)
        ORDER BY EffectiveFrom DESC
    ) cbrh_eff

    -- Effective-date rate for InterviewerBillingType = 2 (SkillChannel)
    OUTER APPLY (
        SELECT TOP 1 BillingRate
        FROM SkillChannelBillingRate
        WHERE IRFId = IRF.id
          AND (EffectiveFrom IS NULL OR EffectiveFrom <= CAST(im.interviewDate AS DATE))
        ORDER BY ISNULL(EffectiveFrom, '1900-01-01') DESC
    ) scbr_eff

    -- Per-interviewer rate (InterviewerBillingType = 3)
    OUTER APPLY (
        SELECT TOP 1 ul_int.id AS UserId
        FROM UserList ul_int
        WHERE ul_int.EmailId = im.interviewer
    ) int_user
    OUTER APPLY (
        SELECT TOP 1 BillingRate
        FROM InterviewerBillingRate
        WHERE IRFId  = IRF.id
          AND Userid = ISNULL(int_user.UserId, 0)
    ) ibr

    OUTER APPLY (
        SELECT SUM(POPendingAmount) AS POPendingAmount
        FROM Revenuedetails
        WHERE Clientid = IRF.ClientId
          AND (Years * 100 + Months) BETWEEN (@FromYear * 100 + @FromMonth) AND (@ToYear * 100 + @ToMonth)
    ) Po
    OUTER APPLY (
        SELECT SUM(InvoiceAmount) AS InvoiceAmount
        FROM Revenuedetails
        WHERE Clientid = IRF.ClientId
          AND (Years * 100 + Months) BETWEEN (@FromYear * 100 + @FromMonth) AND (@ToYear * 100 + @ToMonth)
    ) Inv
    OUTER APPLY (
        SELECT ISNULL(SUM(gi.InvoiceValue), 0) AS GenInvoiceAmount,
               COUNT(gi.id)                    AS GenInvoiceCount
        FROM GeneratedInvoices gi
        WHERE gi.ClientId = cl.id
          AND (gi.InvoiceYear * 100 + gi.InvoiceMonth) BETWEEN (@FromYear * 100 + @FromMonth) AND (@ToYear * 100 + @ToMonth)
          AND gi.Status > 0
    ) GI
    OUTER APPLY (
        SELECT COUNT(DISTINCT IRF2.BillingOwner) AS TotalBillingOwnerCount
        FROM InterviewMeetings im2
        JOIN IRF IRF2          ON IRF2.IRFId      = im2.IRFId
        JOIN InterviewComments ic2 ON ic2.InterviewId = im2.id
        WHERE im2.interviewDate >= @StartDate
          AND im2.interviewDate <  @EndDate
          AND IRF2.ClientId = cl.id
          AND (ic2.MeetingStatus IN (4, 6, 7, 8)
               OR (cl.id = 177 AND ic2.MeetingStatus IN (1, 3, 9)))
          AND ISNULL(im2.RedoInterview, 0) = 0
    ) BOC
    OUTER APPLY (
        SELECT ISNULL(SUM(gi2.InvoiceValue), 0) AS PaidInvoiceAmount
        FROM GeneratedInvoices gi2
        WHERE gi2.ClientId = cl.id
          AND (gi2.InvoiceYear * 100 + gi2.InvoiceMonth) BETWEEN (@FromYear * 100 + @FromMonth) AND (@ToYear * 100 + @ToMonth)
          AND gi2.Status = 5
    ) PI

    WHERE im.interviewDate >= @StartDate
      AND im.interviewDate <  @EndDate
      AND (
            ic.MeetingStatus IN (4, 6, 7, 8)
            OR (cl.id = 177 AND ic.MeetingStatus IN (1, 3, 9))
          )
      AND ISNULL(im.RedoInterview, 0) = 0
    GROUP BY
        cl.id, cl.Name, cl.PerInterviewRate, cl.AIPerInterviewRate, cl.BillingType,
        cl.InterviewerBillingType, cl.BillingCurrency,
        Po.POPendingAmount, Inv.InvoiceAmount, cl.PaymentTerm,
        GI.GenInvoiceAmount, GI.GenInvoiceCount, BOC.TotalBillingOwnerCount,
        PI.PaidInvoiceAmount
END
GO
