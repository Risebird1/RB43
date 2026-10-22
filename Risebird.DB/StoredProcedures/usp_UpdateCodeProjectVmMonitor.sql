IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateCodeProjectVmMonitor'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateCodeProjectVmMonitor
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_UpdateCodeProjectVmMonitor
    @Handle varchar(64),
    @SeenSessionIdsJson nvarchar(max) = NULL,
    @IntegrityFlagsJson nvarchar(max) = NULL,
    @MarkLocked bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CodeProjectVmProvisioning
    SET SeenSessionIdsJson = ISNULL(@SeenSessionIdsJson, SeenSessionIdsJson),
        IntegrityFlagsJson = ISNULL(@IntegrityFlagsJson, IntegrityFlagsJson),
        LockedAtUtc = CASE WHEN @MarkLocked = 1 AND LockedAtUtc IS NULL THEN SYSUTCDATETIME() ELSE LockedAtUtc END,
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE Handle = @Handle;
END
GO
