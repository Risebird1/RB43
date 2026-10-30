IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_OpsToolkit_GetIRFMissingScheduleLink'))
BEGIN
    DROP PROCEDURE dbo.usp_OpsToolkit_GetIRFMissingScheduleLink
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Ops Toolkit: skill channels (IRF) with no CalendlyLink (scheduler link).
-- The link is built from the skill channel's ScheduleInterviewEvent (one-time candidate events have
-- SkillChannel NULL, so they never match). When a channel has several events the active, latest one wins.
-- Rows without an event are returned too (HasScheduleEvent = 0) so they can be fixed in the scheduler first.
CREATE PROCEDURE [dbo].[usp_OpsToolkit_GetIRFMissingScheduleLink]
    @BaseUrl VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        irf.id                                  AS IRFIntId,
        irf.IRFId,
        c.Name                                  AS ClientName,
        irf.SkillCluster,
        irf.PrimarySkillInvite,
        irf.StartDate,
        irf.EndDate,
        ISNULL(irf.IsPaused, 0)                 AS IsPaused,
        irf.CreatedOn,
        ev.EventName,
        ev.Uniqueid                             AS EventUniqueId,
        ISNULL(ev.IsActive, 0)                  AS IsEventActive,
        CAST(CASE WHEN ev.Uniqueid IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasScheduleEvent,
        CASE WHEN ev.Uniqueid IS NOT NULL
             THEN @BaseUrl + '/InterviewScheduler/ScheduleEvent?id=' + ev.Uniqueid END AS SuggestedLink
    FROM IRF irf
    LEFT JOIN Client c ON c.id = irf.ClientId
    OUTER APPLY (
        SELECT TOP 1 sc.EventName, sc.Uniqueid, sc.IsActive
        FROM ScheduleInterviewEvent sc
        WHERE sc.SkillChannel = irf.IRFId
          AND NULLIF(LTRIM(RTRIM(sc.Uniqueid)), '') IS NOT NULL
        ORDER BY ISNULL(sc.IsActive, 0) DESC, sc.id DESC
    ) ev
    WHERE NULLIF(LTRIM(RTRIM(irf.CalendlyLink)), '') IS NULL
    ORDER BY CASE WHEN ev.Uniqueid IS NOT NULL THEN 0 ELSE 1 END, irf.EndDate DESC, irf.id DESC;
END
GO
