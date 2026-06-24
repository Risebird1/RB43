IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientInvoiceFormData'))
    DROP PROCEDURE dbo.usp_GetClientInvoiceFormData
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns client master details + billing owner + completed-interview line items.
-- @BillingOwnerId > 0  => BillingType=1: filter by billing owner
-- @BillingOwnerId = 0  => BillingType=2/3: filter by client only
--
-- Line-item rate is determined by Client.InterviewerBillingType:
--   1 = Client       => AI interviews -> AIPerInterviewRate, others -> PerInterviewRate
--   2 = Skill Channel => SkillChannelBillingRate per IRF, fallback to PerInterviewRate
--   3 = Interviewer  => InterviewerBillingRate per interviewer per IRF
--
-- Special case - Client 177 (Turing):
--   MeetingStatus IN (1, 3, 9) are also included at 30% of the normal rate.
--   They appear as separate line items with Basis suffix "| Partial Billing (30%)".
CREATE PROCEDURE usp_GetClientInvoiceFormData
    @ClientId       INT,
    @Month          INT,
    @Year           INT,
    @BillingOwnerId INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartDate              DATETIME
    DECLARE @EndDate                DATETIME
    DECLARE @InterviewerBillingType INT

    SET @StartDate = DATEFROMPARTS(@Year, @Month, 1)
    SET @EndDate   = DATEADD(MONTH, 1, @StartDate)

    SELECT @InterviewerBillingType = ISNULL(InterviewerBillingType, 1)
    FROM   Client
    WHERE  id = @ClientId

    -- 1. Client master info + default billing owner
    SELECT
        c.id                                    AS ClientId,
        c.Name                                  AS ClientName,
        c.GST                                   AS GSTNumber,
        c.PaymentTerm,
        c.GSTApplicable,
        c.BillingType,
        ISNULL(c.InterviewerBillingType, 1)     AS InterviewerBillingType,
        ISNULL(c.BillingCurrency, 'INR')        AS BillingCurrency,
        c.PerInterviewRate,
        ISNULL(c.AIPerInterviewRate, 0)         AS AIPerInterviewRate,
        ul.EmailId                              AS BillingOwnerEmail,
        ul.id                                   AS BillingOwnerId
    FROM Client c
    LEFT JOIN (
        SELECT TOP 1 IRF.BillingOwner, IRF.ClientId
        FROM IRF
        WHERE IRF.ClientId = @ClientId
          AND IRF.BillingOwner IS NOT NULL
        ORDER BY IRF.BillingOwner
    ) bo ON bo.ClientId = c.id
    LEFT JOIN UserList ul ON ul.id = bo.BillingOwner
    WHERE c.id = @ClientId

    -- 2. Line items
    -- A CTE is used for each branch to pre-compute IsPartial (0/1) so it can
    -- appear as a plain column in both SELECT and GROUP BY, avoiding the
    -- "not contained in aggregate or GROUP BY" error.

    -- ── Branch: Interviewer (3) ───────────────────────────────────────────────
    IF @InterviewerBillingType = 3
    BEGIN
        ;WITH cte AS (
            SELECT
                irf.IRFId,
                irf.JDTiltle,
                im.id           AS MeetingId,
                im.interviewer,
                ul_int.FirstName     AS InterviewerName,
                ibr.BillingRate,
                CASE WHEN @ClientId = 177 AND ic.MeetingStatus IN (1, 3, 9)
                     THEN 1 ELSE 0 END  AS IsPartial
            FROM InterviewMeetings im
            JOIN IRF               ON IRF.IRFId       = im.IRFId
            JOIN InterviewComments ic ON ic.InterviewId = im.id
            JOIN Client c          ON c.id            = IRF.ClientId
            LEFT JOIN UserList ul_int
                   ON ul_int.EmailId = im.interviewer
            LEFT JOIN InterviewerBillingRate ibr
                   ON ibr.IRFId  = IRF.id
                  AND ibr.Userid = ISNULL(ul_int.id, 0)
            WHERE IRF.ClientId = @ClientId
              AND im.interviewDate >= @StartDate
              AND im.interviewDate <  @EndDate
              AND (
                    ic.MeetingStatus IN (4, 6, 7, 8)
                    OR (@ClientId = 177 AND ic.MeetingStatus IN (1, 3, 9))
                  )
              AND ISNULL(im.RedoInterview, 0) = 0
              AND (@BillingOwnerId = 0 OR IRF.BillingOwner = @BillingOwnerId)
        )
        SELECT
            IRFId                                                                AS ProductName,
            ISNULL(JDTiltle, IRFId)                                             AS Description,
            COUNT(MeetingId)                                                     AS Qty,
            CAST(ISNULL(BillingRate, 0) AS DECIMAL(18,2))
                * CASE WHEN IsPartial = 1 THEN CAST(0.30 AS DECIMAL(5,2))
                                          ELSE CAST(1.00 AS DECIMAL(5,2)) END   AS UnitRate,
            COUNT(MeetingId)
                * CAST(ISNULL(BillingRate, 0) AS DECIMAL(18,2))
                * CASE WHEN IsPartial = 1 THEN CAST(0.30 AS DECIMAL(5,2))
                                          ELSE CAST(1.00 AS DECIMAL(5,2)) END   AS LineTotal,
            'Interviewer Rate'
                + CASE WHEN IsPartial = 1 THEN ' | Partial Billing (30%)'
                                          ELSE '' END                           AS Basis
        FROM cte
        GROUP BY IRFId, JDTiltle,BillingRate, IsPartial
        ORDER BY IRFId
    END

    -- ── Branch: Skill Channel (2) ─────────────────────────────────────────────
    ELSE IF @InterviewerBillingType = 2
    BEGIN
        ;WITH cte AS (
            SELECT
                irf.IRFId,
                irf.JDTiltle,
                im.id                                       AS MeetingId,
                ISNULL(scbr.BillingRate, c.PerInterviewRate) AS EffectiveRate,
                CASE WHEN scbr.BillingRate IS NOT NULL
                     THEN 'Skill Channel Rate' ELSE 'Client Rate' END AS RateLabel,
                CASE WHEN @ClientId = 177 AND ic.MeetingStatus IN (1, 3, 9)
                     THEN 1 ELSE 0 END                      AS IsPartial
            FROM InterviewMeetings im
            JOIN IRF               ON IRF.IRFId       = im.IRFId
            JOIN InterviewComments ic ON ic.InterviewId = im.id
            JOIN Client c          ON c.id            = IRF.ClientId
            LEFT JOIN SkillChannelBillingRate scbr
                   ON scbr.IRFId = IRF.id
            WHERE IRF.ClientId = @ClientId
              AND im.interviewDate >= @StartDate
              AND im.interviewDate <  @EndDate
              AND (
                    ic.MeetingStatus IN (4, 6, 7, 8)
                    OR (@ClientId = 177 AND ic.MeetingStatus IN (1, 3, 9))
                  )
              AND ISNULL(im.RedoInterview, 0) = 0
              AND (@BillingOwnerId = 0 OR IRF.BillingOwner = @BillingOwnerId)
        )
        SELECT
            IRFId                                                                AS ProductName,
            ISNULL(JDTiltle, IRFId)                                             AS Description,
            COUNT(MeetingId)                                                     AS Qty,
            CAST(EffectiveRate AS DECIMAL(18,2))
                * CASE WHEN IsPartial = 1 THEN CAST(0.30 AS DECIMAL(5,2))
                                          ELSE CAST(1.00 AS DECIMAL(5,2)) END   AS UnitRate,
            COUNT(MeetingId)
                * CAST(EffectiveRate AS DECIMAL(18,2))
                * CASE WHEN IsPartial = 1 THEN CAST(0.30 AS DECIMAL(5,2))
                                          ELSE CAST(1.00 AS DECIMAL(5,2)) END   AS LineTotal,
            RateLabel
                + CASE WHEN IsPartial = 1 THEN ' | Partial Billing (30%)'
                                          ELSE '' END                           AS Basis
        FROM cte
        GROUP BY IRFId, JDTiltle, EffectiveRate, RateLabel, IsPartial
        ORDER BY IRFId
    END

    -- ── Branch: Client (1) — default ─────────────────────────────────────────
    ELSE
    BEGIN
        ;WITH cte AS (
            SELECT
                irf.IRFId,
                irf.JDTiltle,
                im.id                                       AS MeetingId,
                ISNULL(IRF.IsAIInterview, 0)               AS IsAIInterview,
                ISNULL(c.AIPerInterviewRate, 0)            AS AIRate,
                ISNULL(c.PerInterviewRate,   0)            AS NonAIRate,
                CASE WHEN @ClientId = 177 AND ic.MeetingStatus IN (1, 3, 9)
                     THEN 1 ELSE 0 END                      AS IsPartial
            FROM InterviewMeetings im
            JOIN IRF               ON IRF.IRFId       = im.IRFId
            JOIN InterviewComments ic ON ic.InterviewId = im.id
            JOIN Client c          ON c.id            = IRF.ClientId
            WHERE IRF.ClientId = @ClientId
              AND im.interviewDate >= @StartDate
              AND im.interviewDate <  @EndDate
              AND (
                    ic.MeetingStatus IN (4, 6, 7, 8)
                    OR (@ClientId = 177 AND ic.MeetingStatus IN (1, 3, 9))
                  )
              AND ISNULL(im.RedoInterview, 0) = 0
              AND (@BillingOwnerId = 0 OR IRF.BillingOwner = @BillingOwnerId)
        )
        SELECT
            IRFId                                                                AS ProductName,
            ISNULL(JDTiltle, IRFId)                                             AS Description,
            COUNT(MeetingId)                                                     AS Qty,
            CASE WHEN IsAIInterview = 1 THEN AIRate ELSE NonAIRate END
                * CASE WHEN IsPartial = 1 THEN CAST(0.30 AS DECIMAL(5,2))
                                          ELSE CAST(1.00 AS DECIMAL(5,2)) END   AS UnitRate,
            COUNT(MeetingId)
                * (CASE WHEN IsAIInterview = 1 THEN AIRate ELSE NonAIRate END)
                * CASE WHEN IsPartial = 1 THEN CAST(0.30 AS DECIMAL(5,2))
                                          ELSE CAST(1.00 AS DECIMAL(5,2)) END   AS LineTotal,
            CASE WHEN IsAIInterview = 1 THEN 'AI Interview' ELSE 'Non-AI Interview' END
                + CASE WHEN IsPartial = 1 THEN ' | Partial Billing (30%)'
                                          ELSE '' END                           AS Basis
        FROM cte
        GROUP BY IRFId, JDTiltle, IsAIInterview, AIRate, NonAIRate, IsPartial
        ORDER BY IRFId
    END
END
GO
