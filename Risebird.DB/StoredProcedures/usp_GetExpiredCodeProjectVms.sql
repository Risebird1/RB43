IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetExpiredCodeProjectVms'))
BEGIN
    DROP PROCEDURE dbo.usp_GetExpiredCodeProjectVms
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetExpiredCodeProjectVms
    @MaxLifetimeMinutes int = 120,
    @FailedGraceMinutes int = 10
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Handle FROM CodeProjectVmProvisioning
    WHERE Step <> 'released'
      AND ((Step = 'failed' AND UpdatedAtUtc < DATEADD(minute, -@FailedGraceMinutes, SYSUTCDATETIME()))
           OR CreatedAtUtc < DATEADD(minute, -@MaxLifetimeMinutes, SYSUTCDATETIME()));
END
GO
