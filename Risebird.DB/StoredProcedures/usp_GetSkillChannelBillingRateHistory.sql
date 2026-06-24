IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSkillChannelBillingRateHistory'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSkillChannelBillingRateHistory
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns the full BillingRate history for a SkillChannel (IRF), most recent first.
-- EffectiveTo is derived as the day before the next entry's EffectiveFrom (NULL = still active).
CREATE PROCEDURE [dbo].[usp_GetSkillChannelBillingRateHistory]
    @IRFId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.id,
        s.IRFId,
        irf.IRFId                           AS IRFBusinessKey,
        irf.JDTiltle                        AS JDTitle,
        cl.id                               AS ClientId,
        cl.Name                             AS ClientName,
        s.BillingRate,
        s.EffectiveFrom,
        DATEADD(DAY, -1,
            LEAD(s.EffectiveFrom) OVER (
                PARTITION BY s.IRFId
                ORDER BY ISNULL(s.EffectiveFrom, '1900-01-01') DESC
            )
        )                                   AS EffectiveTo,
        s.CreatedBy,
        s.CreatedOn,
        s.ModifiedOn
    FROM SkillChannelBillingRate s
    JOIN IRF  irf ON irf.id     = s.IRFId
    JOIN Client cl ON cl.id     = irf.ClientId
    WHERE s.IRFId = @IRFId
    ORDER BY ISNULL(s.EffectiveFrom, '1900-01-01') DESC
END
GO
