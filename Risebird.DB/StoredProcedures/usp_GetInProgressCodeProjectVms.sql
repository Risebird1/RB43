IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInProgressCodeProjectVms'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInProgressCodeProjectVms
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetInProgressCodeProjectVms
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Handle FROM CodeProjectVmProvisioning
    WHERE Step NOT IN ('ready', 'failed', 'released')
      AND (LeaseUntilUtc IS NULL OR LeaseUntilUtc < SYSUTCDATETIME());
END
GO
