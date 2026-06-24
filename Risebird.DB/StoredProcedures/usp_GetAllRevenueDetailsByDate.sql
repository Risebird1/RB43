IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllRevenueDetailsByDate'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllRevenueDetailsByDate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Revenue export (Excel) — per-IRF rows.
-- BaseAmount uses the same billing-type + effective-date rate logic as usp_GetRevenueDetailsByDate.
-- CurrentRate: single value from ClientBillingRateHistory (scalar subquery on cl.id).
-- Turing (id=177): MeetingStatus 1/3/9 included at 30%.
CREATE PROCEDURE usp_GetAllRevenueDetailsByDate
    @StartDate DATETIME,
    @EndDate   DATETIME
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
        ISNULL(cl.InterviewerBillingType, 1)                                              AS InterviewerBillingType,
        ISNULL(cl.BillingCurrency, 'INR')                                                 AS BillingCurrency,
        IRF.IRFId,
        ul.EmailId                                                                        AS BillingOwner,

        -- CurrentRate: most recent PerSuccessRate from history as of today, fallback Client.PerInterviewRate.
        -- GSTApplicable: most recent value from history as of today, fallback Client.GSTApplicable.
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

        -- AI interviews always use Client.AIPerInterviewRate.
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
    LEFT JOIN UserList ul  ON IRF.BillingOwner = ul.id

    OUTER APPLY (
        SELECT TOP 1 PerSuccessRate
        FROM ClientBillingRateHistory
        WHERE ClientId     = cl.id
          AND EffectiveFrom <= CAST(im.interviewDate AS DATE)
        ORDER BY EffectiveFrom DESC
    ) cbrh_eff
    OUTER APPLY (
        SELECT TOP 1 BillingRate
        FROM SkillChannelBillingRate
        WHERE IRFId = IRF.id
          AND (EffectiveFrom IS NULL OR EffectiveFrom <= CAST(im.interviewDate AS DATE))
        ORDER BY ISNULL(EffectiveFrom, '1900-01-01') DESC
    ) scbr_eff
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

    WHERE im.interviewDate >= @StartDate
      AND im.interviewDate <  @EndDate
      AND (
            ic.MeetingStatus IN (4, 6, 7, 8)
            OR (cl.id = 177 AND ic.MeetingStatus IN (1, 3, 9))
          )
      AND ISNULL(im.RedoInterview, 0) = 0
    GROUP BY
        cl.id, cl.Name, cl.PerInterviewRate, cl.AIPerInterviewRate,
        cl.InterviewerBillingType, cl.BillingCurrency,
        IRF.IRFId, ul.EmailId
END
GO
