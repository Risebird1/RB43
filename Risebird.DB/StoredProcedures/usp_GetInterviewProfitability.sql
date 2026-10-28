IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewProfitability'))
    DROP PROCEDURE dbo.usp_GetInterviewProfitability
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Raw facts for the Interview Profitability grid (InterviewProfitabilityService turns them into
-- per-interview revenue, cost and margin). Amounts are returned in the currency they are recorded in;
-- the service converts them.
--
-- Result sets:
--   1. One row per interview in the window that matches the filters: revenue inputs, interviewer
--      payout and every cost recorded against the interview itself.
--   2. One row per IRF in result 1: costs recorded against the IRF but not a specific interview
--      (JD/rubric/question AI, unanswered scheduling calls, phone-screen campaign minutes), and how
--      many interviews of that IRF were held in the window to share them.
--   3. One row per day of the window that had held interviews or unattributed AI spend: the
--      interviews held that day (all of them, not only the filtered ones) and AI spend that could
--      not be tied to an interview or IRF.
--   4. The operational cost factors in force at any point in the window.
--   5. Call-cost tables that do not exist on this database, whose costs are therefore left out.
--
-- The shares in 2 and 3 are divided by counts over ALL held interviews, never only the filtered
-- ones, so filtering the grid to one client does not load that client with everyone's overhead.
--
-- Revenue mirrors usp_GetClientInvoiceFormData so the grid agrees with what is invoiced:
-- MeetingStatus 4/6/7/8 is billable, redo interviews are not, the rate depends on
-- Client.InterviewerBillingType, and client 177 bills MeetingStatus 1/3/9 at 30%.
-- Change both procedures together.
--
-- Interviewer payout mirrors usp_FetchFNGenerateInvoiceDetails (penalty, surcharge, QC points,
-- video reward, payment SLA) for InterviewPoints rows that are owed or paid
-- (Submitted, Pending, Advance, Paid). Lapsed points are not paid and are not a cost.
CREATE PROCEDURE usp_GetInterviewProfitability
    @StartDate        DATE,
    @EndDate          DATE,           -- inclusive
    @ClientId         INT          = NULL,
    @IRFIntId         INT          = NULL,
    @InterviewerEmail VARCHAR(200) = NULL,
    @IsAIInterview    BIT          = NULL,
    @IncludeCancelled BIT          = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME = CAST(@StartDate AS DATETIME);
    DECLARE @To   DATETIME = DATEADD(DAY, 1, CAST(@EndDate AS DATETIME));

    -- Every interview in the window, unfiltered: the denominators for shared costs.
    CREATE TABLE #AllMeetings (
        MeetingId    INT          NOT NULL PRIMARY KEY,
        IRFCode      VARCHAR(200) NULL,
        InterviewDay DATE         NOT NULL,
        IsHeld       BIT          NOT NULL
    );

    INSERT INTO #AllMeetings (MeetingId, IRFCode, InterviewDay, IsHeld)
    SELECT im.id,
           im.IRFId,
           CAST(im.interviewDate AS DATE),
           CASE WHEN ISNULL(im.confirmationStatus, '') <> '2' THEN 1 ELSE 0 END
    FROM InterviewMeetings im
    WHERE im.interviewDate >= @From
      AND im.interviewDate <  @To
      AND ISNULL(im.IsDeleted, 0) = 0;

    -- AI-interviewer sessions belong to the interview that carries them (InterviewMeetings.SessionId),
    -- whichever day the interview is on.
    CREATE TABLE #SessionMeetings (
        SessionGuid UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        MeetingId   INT              NOT NULL
    );

    INSERT INTO #SessionMeetings (SessionGuid, MeetingId)
    SELECT s.SessionGuid, MIN(s.MeetingId)
    FROM (
        SELECT TRY_CONVERT(UNIQUEIDENTIFIER, im.SessionId) AS SessionGuid, im.id AS MeetingId
        FROM InterviewMeetings im
        WHERE im.SessionId IS NOT NULL AND im.SessionId <> ''
    ) s
    WHERE s.SessionGuid IS NOT NULL
    GROUP BY s.SessionGuid;

    -- ── 1. Interviews ──────────────────────────────────────────────────────
    CREATE TABLE #M (
        MeetingId          INT              NOT NULL PRIMARY KEY,
        InterviewDate      DATETIME         NULL,
        IRFCode            VARCHAR(200)     NULL,
        IRFIntId           INT              NULL,
        IRFTitle           NVARCHAR(MAX)    NULL,
        ClientId           INT              NULL,
        ClientName         NVARCHAR(500)    NULL,
        CandidateEmail     NVARCHAR(500)    NULL,
        InterviewerEmail   NVARCHAR(500)    NULL,
        InterviewerName    NVARCHAR(500)    NULL,
        InterviewerUserId  INT              NULL,
        IsAIInterview      BIT              NOT NULL,
        MeetingStatus      INT              NULL,
        IsCancelled        BIT              NOT NULL,
        IsRedo             BIT              NOT NULL,
        DurationMinutes    INT              NULL,
        SessionGuid        UNIQUEIDENTIFIER NULL,
        BillingType        INT              NOT NULL,
        UnitRate           DECIMAL(18, 2)   NOT NULL DEFAULT 0,
        BillingFactor      DECIMAL(5, 2)    NOT NULL DEFAULT 0,
        BillingBasis       VARCHAR(100)     NULL,
        RevenueCurrency    VARCHAR(50)      NOT NULL,
        InterviewerPayout  DECIMAL(18, 2)   NOT NULL DEFAULT 0,
        PayoutEntries      INT              NOT NULL DEFAULT 0,
        AIDirectUSD        DECIMAL(18, 6)   NOT NULL DEFAULT 0,
        AIDirectCalls      INT              NOT NULL DEFAULT 0,
        VapiUSD            DECIMAL(18, 4)   NOT NULL DEFAULT 0,
        PlivoCallCost      DECIMAL(18, 4)   NOT NULL DEFAULT 0,
        RecordingUSD       DECIMAL(18, 6)   NOT NULL DEFAULT 0,
        RecordingINR       DECIMAL(18, 6)   NOT NULL DEFAULT 0
    );

    INSERT INTO #M (MeetingId, InterviewDate, IRFCode, IRFIntId, IRFTitle, ClientId, ClientName,
                    CandidateEmail, InterviewerEmail, InterviewerName, InterviewerUserId, IsAIInterview,
                    MeetingStatus, IsCancelled, IsRedo, DurationMinutes, SessionGuid, BillingType,
                    UnitRate, BillingFactor, BillingBasis, RevenueCurrency)
    SELECT
        im.id,
        im.interviewDate,
        im.IRFId,
        irf.id,
        irf.JDTiltle,
        c.id,
        c.Name,
        im.candidateEmail,
        im.interviewer,
        LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))),
        ul.id,
        ISNULL(irf.IsAIInterview, 0),
        ic.MeetingStatus,
        CASE WHEN am.IsHeld = 1 THEN 0 ELSE 1 END,
        ISNULL(im.RedoInterview, 0),
        im.interviewDuration,
        TRY_CONVERT(UNIQUEIDENTIFIER, NULLIF(im.SessionId, '')),
        ISNULL(c.InterviewerBillingType, 1),
        CAST(CASE ISNULL(c.InterviewerBillingType, 1)
                 WHEN 3 THEN ISNULL(ibr.BillingRate, 0)
                 WHEN 2 THEN ISNULL(scbr.BillingRate, ISNULL(c.PerInterviewRate, 0))
                 ELSE CASE WHEN ISNULL(irf.IsAIInterview, 0) = 1
                           THEN ISNULL(c.AIPerInterviewRate, 0)
                           ELSE ISNULL(c.PerInterviewRate, 0) END
             END AS DECIMAL(18, 2)),
        CAST(CASE WHEN ISNULL(im.RedoInterview, 0) = 1 THEN 0
                  WHEN ic.MeetingStatus IN (4, 6, 7, 8) THEN 1.00
                  WHEN c.id = 177 AND ic.MeetingStatus IN (1, 3, 9) THEN 0.30
                  ELSE 0 END AS DECIMAL(5, 2)),
        CASE ISNULL(c.InterviewerBillingType, 1)
            WHEN 3 THEN 'Interviewer Rate'
            WHEN 2 THEN CASE WHEN scbr.BillingRate IS NOT NULL THEN 'Skill Channel Rate' ELSE 'Client Rate' END
            ELSE CASE WHEN ISNULL(irf.IsAIInterview, 0) = 1 THEN 'AI Interview' ELSE 'Non-AI Interview' END
        END
        + CASE WHEN ISNULL(im.RedoInterview, 0) = 0 AND c.id = 177 AND ic.MeetingStatus IN (1, 3, 9)
               THEN ' | Partial Billing (30%)' ELSE '' END,
        ISNULL(NULLIF(c.BillingCurrency, ''), 'INR')
    FROM #AllMeetings am
    JOIN InterviewMeetings im ON im.id = am.MeetingId
    LEFT JOIN IRF irf ON irf.IRFId = im.IRFId
    LEFT JOIN Client c ON c.id = irf.ClientId
    OUTER APPLY (SELECT TOP 1 x.MeetingStatus FROM InterviewComments x
                 WHERE x.InterviewId = im.id ORDER BY x.id DESC) ic
    OUTER APPLY (SELECT TOP 1 u.id, u.FirstName, u.LastName FROM UserList u
                 WHERE u.EmailId = im.interviewer ORDER BY u.id) ul
    OUTER APPLY (SELECT TOP 1 b.BillingRate FROM InterviewerBillingRate b
                 WHERE b.IRFId = irf.id AND b.Userid = ISNULL(ul.id, 0) ORDER BY b.id DESC) ibr
    OUTER APPLY (SELECT TOP 1 s.BillingRate FROM SkillChannelBillingRate s
                 WHERE s.IRFId = irf.id ORDER BY s.id DESC) scbr
    WHERE (@IncludeCancelled = 1 OR am.IsHeld = 1)
      AND (@ClientId IS NULL OR irf.ClientId = @ClientId)
      AND (@IRFIntId IS NULL OR irf.id = @IRFIntId)
      AND (@InterviewerEmail IS NULL OR im.interviewer = @InterviewerEmail)
      AND (@IsAIInterview IS NULL OR ISNULL(irf.IsAIInterview, 0) = @IsAIInterview);

    -- Interviewer payout (net of penalty, plus surcharge / QC / video reward, minus payment SLA).
    -- InterviewPoints is read once for every interview in #M. Each entry is worked out first and
    -- summed outside: SQL Server does not allow an aggregate over an expression that mixes these
    -- rows with the IRF and meeting. When the IRF no longer exists there is no surcharge, so the
    -- entry is taken at face value.
    UPDATE m
    SET InterviewerPayout = ISNULL(p.Payout, 0),
        PayoutEntries     = ISNULL(p.Entries, 0)
    FROM #M m
    JOIN (
        SELECT e.MeetingId, SUM(e.Amount) AS Payout, COUNT(*) AS Entries
        FROM (
            SELECT m2.MeetingId,
                   CASE WHEN ip.Points >= 0 THEN ip.Points - (ip.Points * ISNULL(ip.PenaltyAmount, 0) / 100)
                        ELSE ip.Points END
                 + CASE WHEN ti.Surcharge > 0 AND irf.IsNewSkillChannel = 1 AND ip.Points > 0 THEN ti.Surcharge
                        WHEN ti.Surcharge = 0 AND irf.IsNewSkillChannel = 1 AND ip.Points > 0 THEN ISNULL(im.Surcharge, 0)
                        ELSE 0 END
                 + ISNULL(ip.QCPoints, 0)
                 + (ip.Points * ISNULL(ip.VideoReward, 0) / 100)
                 - (ip.Points * ISNULL(ip.PaymentSLA, 0) / 100) AS Amount
            FROM #M m2
            JOIN InterviewPoints ip ON ip.Interviewid = m2.MeetingId
            JOIN InterviewMeetings im ON im.id = m2.MeetingId
            LEFT JOIN IRF irf ON irf.id = m2.IRFIntId
            OUTER APPLY (SELECT TOP 1 t.Surcharge FROM TransInterviewer t
                         WHERE t.IRFId = irf.id AND t.UserId = ip.userid ORDER BY t.id DESC) ti
            WHERE ip.Status IN ('Submitted', 'Pending', 'Advance', 'Paid')
        ) e
        GROUP BY e.MeetingId
    ) p ON p.MeetingId = m.MeetingId;

    -- The cost tables below are each read once for all interviews in #M and joined back, rather
    -- than looked up interview by interview: OpenAIApiLogs is large, and a per-interview lookup
    -- scans it once per interview whenever no index on the lookup column is available.

    -- AI spend logged against the interview, or against its AI-interviewer session. If two
    -- interviews in the report carry the same session, its spend is counted once, on the earlier one.
    CREATE TABLE #AIByMeeting (MeetingId INT NOT NULL PRIMARY KEY, Usd DECIMAL(18, 6) NOT NULL, Calls INT NOT NULL);
    INSERT INTO #AIByMeeting (MeetingId, Usd, Calls)
    SELECT l.MeetingId, ISNULL(SUM(l.EstimatedCostUSD), 0), COUNT(*)
    FROM OpenAIApiLogs l
    WHERE l.MeetingId IN (SELECT MeetingId FROM #M)
    GROUP BY l.MeetingId;

    CREATE TABLE #AIBySession (SessionGuid UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, Usd DECIMAL(18, 6) NOT NULL, Calls INT NOT NULL);
    INSERT INTO #AIBySession (SessionGuid, Usd, Calls)
    SELECT l.SessionId, ISNULL(SUM(l.EstimatedCostUSD), 0), COUNT(*)
    FROM OpenAIApiLogs l
    WHERE l.MeetingId IS NULL
      AND l.SessionId IN (SELECT SessionGuid FROM #M WHERE SessionGuid IS NOT NULL)
    GROUP BY l.SessionId;

    UPDATE m
    SET AIDirectUSD   = ISNULL(bm.Usd, 0) + ISNULL(bs.Usd, 0),
        AIDirectCalls = ISNULL(bm.Calls, 0) + ISNULL(bs.Calls, 0)
    FROM #M m
    LEFT JOIN #AIByMeeting bm ON bm.MeetingId = m.MeetingId
    LEFT JOIN (SELECT SessionGuid, MIN(MeetingId) AS MeetingId FROM #M
               WHERE SessionGuid IS NOT NULL GROUP BY SessionGuid) sm
           ON sm.SessionGuid = m.SessionGuid AND sm.MeetingId = m.MeetingId
    LEFT JOIN #AIBySession bs ON bs.SessionGuid = sm.SessionGuid;

    -- Voice and telephony. These tables arrive with features that are not on every database yet
    -- (PlivoCallLogs is not on production, for one), so each is read only if it exists, through
    -- dynamic SQL so that a missing table cannot stop the procedure from compiling or running.
    -- A missing table counts as no cost and is named in result 5 so the page can say so.
    -- Rows with a MeetingId are recorded against an interview; rows with only an IRFIntId are
    -- recorded against the IRF and not a specific interview, inside the window.
    CREATE TABLE #CallCost (
        Source    VARCHAR(20)    NOT NULL,
        MeetingId INT            NULL,
        IRFIntId  INT            NULL,
        Amount    DECIMAL(18, 6) NOT NULL DEFAULT 0,  -- USD, except Plivo (PlivoCallCostCurrency)
        Inr       DECIMAL(18, 6) NOT NULL DEFAULT 0,  -- recordings priced in INR
        Seconds   BIGINT         NOT NULL DEFAULT 0
    );
    CREATE TABLE #MissingSources (TableName SYSNAME NOT NULL);

    IF OBJECT_ID(N'dbo.VapiCallLogs', N'U') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            INSERT INTO #CallCost (Source, MeetingId, Amount)
            SELECT ''Vapi'', x.InterviewId, ISNULL(SUM(x.CallCost), 0)
            FROM dbo.VapiCallLogs x
            WHERE x.InterviewId IN (SELECT MeetingId FROM #M)
            GROUP BY x.InterviewId;';

        IF COL_LENGTH(N'dbo.VapiCallLogs', N'IRFId') IS NOT NULL
            EXEC sp_executesql N'
                INSERT INTO #CallCost (Source, IRFIntId, Amount)
                SELECT ''Vapi'', x.IRFId, ISNULL(SUM(x.CallCost), 0)
                FROM dbo.VapiCallLogs x
                WHERE x.InterviewId IS NULL
                  AND x.IRFId IN (SELECT IRFIntId FROM #M WHERE IRFIntId IS NOT NULL)
                  AND x.CreatedAt >= @From AND x.CreatedAt < @To
                GROUP BY x.IRFId;',
                N'@From DATETIME, @To DATETIME', @From, @To;
    END
    ELSE INSERT INTO #MissingSources VALUES (N'VapiCallLogs');

    IF OBJECT_ID(N'dbo.PlivoCallLogs', N'U') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            INSERT INTO #CallCost (Source, MeetingId, Amount)
            SELECT ''Plivo'', x.InterviewId, ISNULL(SUM(x.CallCost), 0)
            FROM dbo.PlivoCallLogs x
            WHERE x.InterviewId IN (SELECT MeetingId FROM #M)
            GROUP BY x.InterviewId;

            INSERT INTO #CallCost (Source, IRFIntId, Amount)
            SELECT ''Plivo'', x.IRFId, ISNULL(SUM(x.CallCost), 0)
            FROM dbo.PlivoCallLogs x
            WHERE x.InterviewId IS NULL
              AND x.IRFId IN (SELECT IRFIntId FROM #M WHERE IRFIntId IS NOT NULL)
              AND x.CreatedAt >= @From AND x.CreatedAt < @To
            GROUP BY x.IRFId;',
            N'@From DATETIME, @To DATETIME', @From, @To;
    END
    ELSE INSERT INTO #MissingSources VALUES (N'PlivoCallLogs');

    IF OBJECT_ID(N'dbo.PlivoCXCallLogs', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.PlivoCXCallLogs', N'IRFId') IS NOT NULL
        EXEC sp_executesql N'
            INSERT INTO #CallCost (Source, IRFIntId, Seconds)
            SELECT ''PlivoCX'', x.IRFId, ISNULL(SUM(CAST(x.DurationSeconds AS BIGINT)), 0)
            FROM dbo.PlivoCXCallLogs x
            WHERE x.IRFId IN (SELECT IRFIntId FROM #M WHERE IRFIntId IS NOT NULL)
              AND x.CreatedAt >= @From AND x.CreatedAt < @To
            GROUP BY x.IRFId;',
            N'@From DATETIME, @To DATETIME', @From, @To;
    ELSE INSERT INTO #MissingSources VALUES (N'PlivoCXCallLogs');

    IF OBJECT_ID(N'dbo.CallRecordingData', N'U') IS NOT NULL
        EXEC sp_executesql N'
            INSERT INTO #CallCost (Source, MeetingId, Amount, Inr)
            SELECT ''Recording'', x.InterviewId,
                   ISNULL(SUM(CASE WHEN ISNULL(UPPER(x.PriceUnit), ''USD'') = ''INR'' THEN 0
                                   ELSE ABS(ISNULL(TRY_CAST(x.Price AS DECIMAL(18, 6)), 0)) END), 0),
                   ISNULL(SUM(CASE WHEN UPPER(x.PriceUnit) = ''INR''
                                   THEN ABS(ISNULL(TRY_CAST(x.Price AS DECIMAL(18, 6)), 0)) ELSE 0 END), 0)
            FROM dbo.CallRecordingData x
            WHERE x.InterviewId IN (SELECT MeetingId FROM #M)
            GROUP BY x.InterviewId;';
    ELSE INSERT INTO #MissingSources VALUES (N'CallRecordingData');

    UPDATE m
    SET VapiUSD       = ISNULL(c.Vapi, 0),
        PlivoCallCost = ISNULL(c.Plivo, 0),
        RecordingUSD  = ISNULL(c.RecordingUsd, 0),
        RecordingINR  = ISNULL(c.RecordingInr, 0)
    FROM #M m
    JOIN (SELECT MeetingId,
                 SUM(CASE WHEN Source = 'Vapi'      THEN Amount ELSE 0 END) AS Vapi,
                 SUM(CASE WHEN Source = 'Plivo'     THEN Amount ELSE 0 END) AS Plivo,
                 SUM(CASE WHEN Source = 'Recording' THEN Amount ELSE 0 END) AS RecordingUsd,
                 SUM(CASE WHEN Source = 'Recording' THEN Inr ELSE 0 END)    AS RecordingInr
          FROM #CallCost
          WHERE MeetingId IS NOT NULL
          GROUP BY MeetingId) c ON c.MeetingId = m.MeetingId;

    -- AI spend in the window that is not tied to an interview or its session, per day and IRF
    -- (IRFId NULL = not tied to anything). Read once; results 2 and 3 both use it.
    SELECT CAST(l.LogDate AS DATE) AS [Day], l.IRFId, SUM(l.EstimatedCostUSD) AS Usd
    INTO #AIUnattributed
    FROM OpenAIApiLogs l
    WHERE l.LogDate >= @From AND l.LogDate < @To
      AND l.MeetingId IS NULL
      AND (l.SessionId IS NULL
           OR NOT EXISTS (SELECT 1 FROM #SessionMeetings s WHERE s.SessionGuid = l.SessionId))
    GROUP BY CAST(l.LogDate AS DATE), l.IRFId;

    SELECT MeetingId, InterviewDate, IRFCode, IRFIntId, IRFTitle, ClientId, ClientName, CandidateEmail,
           InterviewerEmail, InterviewerName, IsAIInterview, MeetingStatus, IsCancelled, IsRedo,
           DurationMinutes, BillingType, UnitRate, BillingFactor, BillingBasis, RevenueCurrency,
           InterviewerPayout, PayoutEntries, AIDirectUSD, AIDirectCalls, VapiUSD, PlivoCallCost,
           RecordingUSD, RecordingINR
    FROM #M
    ORDER BY InterviewDate, MeetingId;

    -- ── 2. IRF-level costs ─────────────────────────────────────────────────
    SELECT
        i.IRFIntId,
        i.IRFCode,
        ISNULL((SELECT SUM(a.Usd) FROM #AIUnattributed a WHERE a.IRFId = i.IRFIntId), 0) AS AIUnattributedUSD,
        ISNULL((SELECT SUM(c.Amount) FROM #CallCost c
                WHERE c.Source = 'Vapi' AND c.MeetingId IS NULL AND c.IRFIntId = i.IRFIntId), 0) AS VapiUnattributedUSD,
        ISNULL((SELECT SUM(c.Amount) FROM #CallCost c
                WHERE c.Source = 'Plivo' AND c.MeetingId IS NULL AND c.IRFIntId = i.IRFIntId), 0) AS PlivoUnattributedCost,
        ISNULL((SELECT SUM(c.Seconds) FROM #CallCost c
                WHERE c.Source = 'PlivoCX' AND c.IRFIntId = i.IRFIntId), 0) AS PlivoCXSeconds,
        (SELECT COUNT(*) FROM #AllMeetings am WHERE am.IRFCode = i.IRFCode AND am.IsHeld = 1) AS HeldMeetings
    FROM (SELECT DISTINCT IRFIntId, IRFCode FROM #M WHERE IRFIntId IS NOT NULL) i;

    -- ── 3. Days ────────────────────────────────────────────────────────────
    -- OpenAIApiLogs.LogDate is UTC; interview dates are local, so a call made just after local
    -- midnight lands on the previous day. Over a window of days the difference washes out.
    ;WITH Held AS (
        SELECT InterviewDay AS [Day], COUNT(*) AS HeldMeetings
        FROM #AllMeetings
        WHERE IsHeld = 1
        GROUP BY InterviewDay
    ),
    SharedAI AS (
        SELECT [Day], SUM(Usd) AS Usd
        FROM #AIUnattributed
        WHERE IRFId IS NULL
        GROUP BY [Day]
    )
    SELECT COALESCE(h.[Day], a.[Day]) AS [Day],
           ISNULL(h.HeldMeetings, 0)  AS HeldMeetings,
           ISNULL(a.Usd, 0)           AS SharedAIUSD
    FROM Held h
    FULL OUTER JOIN SharedAI a ON a.[Day] = h.[Day]
    ORDER BY 1;

    -- ── 4. Operational cost factors ────────────────────────────────────────
    SELECT Id, Name, Category, Amount, Currency, Basis, EffectiveFrom, EffectiveTo, IsActive, Notes
    FROM OperationalCostFactor
    WHERE IsActive = 1
      AND EffectiveFrom <= @EndDate
      AND (EffectiveTo IS NULL OR EffectiveTo >= @StartDate)
    ORDER BY Category, Name;

    -- ── 5. Call-cost tables not on this database (their costs are left out) ─
    SELECT TableName FROM #MissingSources ORDER BY TableName;

    DROP TABLE #MissingSources;
    DROP TABLE #CallCost;
    DROP TABLE #AIUnattributed;
    DROP TABLE #AIBySession;
    DROP TABLE #AIByMeeting;
    DROP TABLE #M;
    DROP TABLE #SessionMeetings;
    DROP TABLE #AllMeetings;
END
GO
