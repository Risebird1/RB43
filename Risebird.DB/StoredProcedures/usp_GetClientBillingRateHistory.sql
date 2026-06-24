IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientBillingRateHistory'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientBillingRateHistory
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns the full PerSuccessRate + GSTApplicable history for a client (most recent first).
-- EffectiveTo is derived as the day before the next entry's EffectiveFrom (NULL = still active).
-- GSTApplicable NULL means the Client table value was in effect (backward compat).
CREATE PROCEDURE [dbo].[usp_GetClientBillingRateHistory]
    @ClientId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        h.Id,
        h.ClientId,
        cl.Name                             AS ClientName,
        h.PerSuccessRate,
        -- Resolve GSTApplicable: history value if set, else inherit from Client table
        ISNULL(h.GSTApplicable, cl.GSTApplicable) AS GSTApplicable,
        h.EffectiveFrom,
        DATEADD(DAY, -1,
            LEAD(h.EffectiveFrom) OVER (
                PARTITION BY h.ClientId
                ORDER BY h.EffectiveFrom DESC
            )
        )                                   AS EffectiveTo,
        h.CreatedBy,
        h.CreatedOn
    FROM ClientBillingRateHistory h
    JOIN Client cl ON cl.id = h.ClientId
    WHERE h.ClientId = @ClientId
    ORDER BY h.EffectiveFrom DESC
END
GO
