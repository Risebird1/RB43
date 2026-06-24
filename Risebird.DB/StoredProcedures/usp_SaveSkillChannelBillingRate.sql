IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveSkillChannelBillingRate'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveSkillChannelBillingRate
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Saves a SkillChannel billing rate for an IRF with an effective date.
-- InterviewerBillingType = 2 (Skill Channel) only.
--
-- Each call inserts a new history row so that past interviews retain the
-- rate that was active at the time.  If the same IRFId + EffectiveFrom
-- already exists the row is updated in-place.
--
-- Backward compatibility: when @EffectiveFrom is NULL the legacy
-- "one row per IRF" behaviour is preserved (update-or-insert with no date).
CREATE PROCEDURE [dbo].[usp_SaveSkillChannelBillingRate]
    @IRFId         INT,
    @BillingRate   INT,
    @EffectiveFrom DATE         = NULL,
    @CreatedBy     VARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @EffectiveFrom IS NULL
    BEGIN
        -- Legacy path: maintain single row per IRF (no effective-date tracking)
        IF EXISTS (SELECT TOP 1 id FROM SkillChannelBillingRate WHERE IRFId = @IRFId AND EffectiveFrom IS NULL)
        BEGIN
            UPDATE SkillChannelBillingRate
            SET BillingRate = @BillingRate,
                ModifiedOn  = GETDATE()
            WHERE IRFId = @IRFId
              AND EffectiveFrom IS NULL
        END
        ELSE
        BEGIN
            INSERT INTO SkillChannelBillingRate (IRFId, BillingRate, EffectiveFrom, CreatedBy, CreatedOn)
            VALUES (@IRFId, @BillingRate, NULL, @CreatedBy, GETDATE())
        END
    END
    ELSE
    BEGIN
        -- Effective-date path: insert new history row (or update same-date entry)
        IF EXISTS (
            SELECT TOP 1 id FROM SkillChannelBillingRate
            WHERE IRFId = @IRFId AND EffectiveFrom = @EffectiveFrom
        )
        BEGIN
            UPDATE SkillChannelBillingRate
            SET BillingRate = @BillingRate,
                ModifiedOn  = GETDATE()
            WHERE IRFId        = @IRFId
              AND EffectiveFrom = @EffectiveFrom
        END
        ELSE
        BEGIN
            INSERT INTO SkillChannelBillingRate (IRFId, BillingRate, EffectiveFrom, CreatedBy, CreatedOn)
            VALUES (@IRFId, @BillingRate, @EffectiveFrom, @CreatedBy, GETDATE())
        END
    END
END
GO
