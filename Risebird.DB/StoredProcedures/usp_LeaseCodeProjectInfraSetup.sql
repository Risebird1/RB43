IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_LeaseCodeProjectInfraSetup'))
BEGIN
    DROP PROCEDURE dbo.usp_LeaseCodeProjectInfraSetup
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_LeaseCodeProjectInfraSetup
    @Id int,
    @LeaseSeconds int = 180
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CodeProjectInfraSetupRuns
    SET LeaseUntilUtc = DATEADD(second, @LeaseSeconds, SYSUTCDATETIME())
    WHERE Id = @Id AND Status = 'running'
      AND (LeaseUntilUtc IS NULL OR LeaseUntilUtc < SYSUTCDATETIME());
    SELECT @@ROWCOUNT AS Leased;
END
GO
