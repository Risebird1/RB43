IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientInvoiceFormData'))
    DROP PROCEDURE dbo.usp_GetClientInvoiceFormData
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns client master details + billing owner + completed-interview line items.
-- @BillingOwnerId > 0  => BillingType=1: filter line items by that billing owner + clientId
-- @BillingOwnerId = 0  => BillingType=2/3: line items for the whole client
--
-- Line-item rate is determined by Client.InterviewerBillingType:
--   1 = Client      => AI interviews use AIPerInterviewRate, others use PerInterviewRate
--   2 = Skill Channel => SkillChannelBillingRate per IRF, fallback to PerInterviewRate
--   3 = Interviewer  => InterviewerBillingRate per interviewer per IRF, grouped by IRF + interviewer
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

    -- 2. Line items — rate depends on InterviewerBillingType

    -- InterviewerBillingType = 3 (Interviewer):
    --   Group by IRF + interviewer, rate from InterviewerBillingRate per interviewer per IRF
    IF @InterviewerBillingType = 3
    BEGIN
        SELECT
            irf.IRFId                                                AS ProductName,
            ISNULL(irf.JDTiltle, irf.IRFId)                         AS Description,
            COUNT(im.id)                                             AS Qty,
            ISNULL(ibr.BillingRate, 0)                              AS UnitRate,
            COUNT(im.id) * ISNULL(ibr.BillingRate, 0)              AS LineTotal,
            ISNULL(ul_int.FirstName, im.interviewer)                     AS Basis
        FROM InterviewMeetings im
        JOIN IRF               ON IRF.IRFId       = im.IRFId
        JOIN InterviewComments ic ON ic.InterviewId = im.id
        JOIN Client c          ON c.id            = IRF.ClientId
        LEFT JOIN UserList ul_int
               ON ul_int.EmailId = im.interviewer
        LEFT JOIN InterviewerBillingRate ibr
               ON ibr.IRFId   = IRF.id
              AND ibr.Userid  = ISNULL(ul_int.id, 0)
        WHERE IRF.ClientId = @ClientId
          AND im.interviewDate >= @StartDate
          AND im.interviewDate <  @EndDate
          AND (ic.MeetingStatus = 4 OR ic.MeetingStatus = 6
               OR ic.MeetingStatus = 7 OR ic.MeetingStatus = 8)
          AND ISNULL(im.RedoInterview, 0) = 0
          AND (@BillingOwnerId = 0 OR IRF.BillingOwner = @BillingOwnerId)
        GROUP BY irf.IRFId, irf.JDTiltle, im.interviewer, ul_int.FirstName, ibr.BillingRate
        ORDER BY irf.IRFId
    END

    -- InterviewerBillingType = 2 (Skill Channel):
    --   Group by IRF, rate from SkillChannelBillingRate; fallback to Client.PerInterviewRate
    ELSE IF @InterviewerBillingType = 2
    BEGIN
        SELECT
            irf.IRFId                                                                          AS ProductName,
            ISNULL(irf.JDTiltle, irf.IRFId)                                                   AS Description,
            COUNT(im.id)                                                                       AS Qty,
            ISNULL(scbr.BillingRate, c.PerInterviewRate)                                      AS UnitRate,
            COUNT(im.id) * ISNULL(scbr.BillingRate, c.PerInterviewRate)                       AS LineTotal,
            CASE WHEN scbr.BillingRate IS NOT NULL
                 THEN 'Skill Channel Rate'
                 ELSE 'Client Rate'
            END                                                                                AS Basis
        FROM InterviewMeetings im
        JOIN IRF               ON IRF.IRFId       = im.IRFId
        JOIN InterviewComments ic ON ic.InterviewId = im.id
        JOIN Client c          ON c.id            = IRF.ClientId
        LEFT JOIN SkillChannelBillingRate scbr
               ON scbr.IRFId = IRF.id
        WHERE IRF.ClientId = @ClientId
          AND im.interviewDate >= @StartDate
          AND im.interviewDate <  @EndDate
          AND (ic.MeetingStatus = 4 OR ic.MeetingStatus = 6
               OR ic.MeetingStatus = 7 OR ic.MeetingStatus = 8)
          AND ISNULL(im.RedoInterview, 0) = 0
          AND (@BillingOwnerId = 0 OR IRF.BillingOwner = @BillingOwnerId)
        GROUP BY irf.IRFId, irf.JDTiltle, scbr.BillingRate, c.PerInterviewRate
        ORDER BY irf.IRFId
    END

    -- InterviewerBillingType = 1 (Client) — default:
    --   AI interviews use AIPerInterviewRate, non-AI use PerInterviewRate
    ELSE
    BEGIN
        SELECT
            irf.IRFId                                                                         AS ProductName,
            ISNULL(irf.JDTiltle, irf.IRFId)                                                  AS Description,
            COUNT(im.id)                                                                      AS Qty,
            CASE WHEN ISNULL(IRF.IsAIInterview, 0) = 1
                 THEN ISNULL(c.AIPerInterviewRate, 0)
                 ELSE ISNULL(c.PerInterviewRate, 0)
            END                                                                               AS UnitRate,
            COUNT(im.id) * CASE WHEN ISNULL(IRF.IsAIInterview, 0) = 1
                                THEN ISNULL(c.AIPerInterviewRate, 0)
                                ELSE ISNULL(c.PerInterviewRate, 0)
                           END                                                                AS LineTotal,
            CASE WHEN ISNULL(IRF.IsAIInterview, 0) = 1
                 THEN 'AI Interview'
                 ELSE 'Non-AI Interview'
            END                                                                               AS Basis
        FROM InterviewMeetings im
        JOIN IRF               ON IRF.IRFId       = im.IRFId
        JOIN InterviewComments ic ON ic.InterviewId = im.id
        JOIN Client c          ON c.id            = IRF.ClientId
        WHERE IRF.ClientId = @ClientId
          AND im.interviewDate >= @StartDate
          AND im.interviewDate <  @EndDate
          AND (ic.MeetingStatus = 4 OR ic.MeetingStatus = 6
               OR ic.MeetingStatus = 7 OR ic.MeetingStatus = 8)
          AND ISNULL(im.RedoInterview, 0) = 0
          AND (@BillingOwnerId = 0 OR IRF.BillingOwner = @BillingOwnerId)
        GROUP BY irf.IRFId, irf.JDTiltle, IRF.IsAIInterview, c.PerInterviewRate, c.AIPerInterviewRate
        ORDER BY irf.IRFId
    END
END
GO
