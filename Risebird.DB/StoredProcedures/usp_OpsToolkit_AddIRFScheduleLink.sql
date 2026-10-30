IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_OpsToolkit_AddIRFScheduleLink'))
BEGIN
    DROP PROCEDURE dbo.usp_OpsToolkit_AddIRFScheduleLink
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Ops Toolkit: fills IRF.CalendlyLink from the skill channel's ScheduleInterviewEvent.
-- @IRFIntId = IRF.id for one channel, or NULL to fix every channel that is missing a link and has an event.
-- Only touches rows whose link is still empty, and logs each change in OpsToolkitActionLog.
CREATE PROCEDURE [dbo].[usp_OpsToolkit_AddIRFScheduleLink]
    @IRFIntId    INT = NULL,
    @BaseUrl     VARCHAR(200),
    @PerformedBy VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Updated TABLE (IRFIntId INT, IRFId VARCHAR(200), Link VARCHAR(500));

    BEGIN TRY
        BEGIN TRAN;

        UPDATE irf
        SET irf.CalendlyLink = @BaseUrl + '/InterviewScheduler/ScheduleEvent?id=' + ev.Uniqueid
        OUTPUT inserted.id, inserted.IRFId, inserted.CalendlyLink INTO @Updated (IRFIntId, IRFId, Link)
        FROM IRF irf
        CROSS APPLY (
            SELECT TOP 1 sc.Uniqueid
            FROM ScheduleInterviewEvent sc
            WHERE sc.SkillChannel = irf.IRFId
              AND NULLIF(LTRIM(RTRIM(sc.Uniqueid)), '') IS NOT NULL
            ORDER BY ISNULL(sc.IsActive, 0) DESC, sc.id DESC
        ) ev
        WHERE NULLIF(LTRIM(RTRIM(irf.CalendlyLink)), '') IS NULL
          AND (@IRFIntId IS NULL OR irf.id = @IRFIntId);

        INSERT INTO OpsToolkitActionLog (ActionType, Reason, PerformedBy)
        SELECT 'AddIRFScheduleLink', 'IRF ' + u.IRFId + ' (id ' + CAST(u.IRFIntId AS VARCHAR(20)) + '): ' + u.Link, @PerformedBy
        FROM @Updated u;

        COMMIT TRAN;

        DECLARE @Count INT = (SELECT COUNT(1) FROM @Updated);
        SELECT CAST(CASE WHEN @Count > 0 THEN 1 ELSE 0 END AS BIT) AS Success,
               CASE WHEN @Count = 0 THEN 'Nothing updated. The link is already set or the skill channel has no scheduler event.'
                    WHEN @Count = 1 THEN 'Scheduler link added.'
                    ELSE CAST(@Count AS VARCHAR(20)) + ' scheduler links added.' END AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO
