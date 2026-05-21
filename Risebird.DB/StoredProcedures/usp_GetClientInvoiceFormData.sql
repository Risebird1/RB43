IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientInvoiceFormData'))
    DROP PROCEDURE dbo.usp_GetClientInvoiceFormData
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns client master details + billing owner + completed-interview line items for the given month/year.
-- Line items are fetched only when BillingType = 1 (Billing Owner based).
CREATE PROCEDURE usp_GetClientInvoiceFormData
    @ClientId  INT,
    @Month     INT,
    @Year      INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartDate DATETIME = DATEFROMPARTS(@Year, @Month, 1)
    DECLARE @EndDate   DATETIME = DATEADD(MONTH, 1, @StartDate)

    -- 1. Client master info
    SELECT
        c.id             AS ClientId,
        c.Name           AS ClientName,
        c.GST            AS GSTNumber,
        c.PaymentTerm,
        c.GSTApplicable,
        c.BillingType,
        c.PerInterviewRate,
        ul.EmailId       AS BillingOwnerEmail,
        ul.id            AS BillingOwnerId
    FROM Client c
    LEFT JOIN (
        SELECT DISTINCT TOP 1 IRF.BillingOwner, IRF.ClientId
        FROM IRF
        WHERE IRF.ClientId = @ClientId AND IRF.BillingOwner IS NOT NULL
        ORDER BY IRF.BillingOwner
    ) bo ON bo.ClientId = c.id
    LEFT JOIN UserList ul ON ul.id = bo.BillingOwner
    WHERE c.id = @ClientId

    -- 2. Line items: completed interviews grouped by IRF for the selected month/year
    SELECT
        irf.IRFId                                    AS ProductName,
        ISNULL(irf.JDTiltle, irf.IRFId)              AS Description,
        COUNT(im.id)                                 AS Qty,
        ISNULL(c.PerInterviewRate, 0)                AS UnitRate,
        COUNT(im.id) * ISNULL(c.PerInterviewRate, 0) AS LineTotal,
        'Wallet'                                     AS Basis
    FROM InterviewMeetings im
    JOIN IRF ON IRF.IRFId = im.IRFId
    JOIN InterviewComments ic ON ic.InterviewId = im.id
    JOIN Client c ON c.id = IRF.ClientId
    WHERE IRF.ClientId = @ClientId
      AND im.interviewDate >= @StartDate
      AND im.interviewDate <  @EndDate
      AND (ic.MeetingStatus = 4 OR ic.MeetingStatus = 6
           OR ic.MeetingStatus = 7 OR ic.MeetingStatus = 8)
    GROUP BY irf.IRFId, irf.JDTiltle, c.PerInterviewRate
    ORDER BY irf.IRFId
END
GO
