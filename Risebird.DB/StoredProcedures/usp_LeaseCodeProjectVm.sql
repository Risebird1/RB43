IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_LeaseCodeProjectVm'))
BEGIN
    DROP PROCEDURE dbo.usp_LeaseCodeProjectVm
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_LeaseCodeProjectVm
    @Handle varchar(64),
    @LeaseSeconds int = 60
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CodeProjectVmProvisioning
    SET LeaseUntilUtc = DATEADD(second, @LeaseSeconds, SYSUTCDATETIME())
    WHERE Handle = @Handle
      AND Step NOT IN ('ready', 'failed', 'released')
      AND (LeaseUntilUtc IS NULL OR LeaseUntilUtc < SYSUTCDATETIME());
    SELECT @@ROWCOUNT AS Leased;
END
GO
