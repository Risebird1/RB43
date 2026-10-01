IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateCodeProjectVm'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateCodeProjectVm
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_UpdateCodeProjectVm
    @Handle varchar(64),
    @Step varchar(20),
    @Message nvarchar(1000) = NULL,
    @VMPropertyId int = NULL,
    @ConnectUrl nvarchar(1000) = NULL,
    @ClearProjectZipUrl bit = 0,
    @ReleaseLease bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CodeProjectVmProvisioning
    SET Step = @Step,
        Message = @Message,
        VMPropertyId = ISNULL(@VMPropertyId, VMPropertyId),
        ConnectUrl = ISNULL(@ConnectUrl, ConnectUrl),
        ProjectZipUrl = CASE WHEN @ClearProjectZipUrl = 1 THEN NULL ELSE ProjectZipUrl END,
        LeaseUntilUtc = CASE WHEN @ReleaseLease = 1 THEN NULL ELSE LeaseUntilUtc END,
        UpdatedAtUtc = SYSUTCDATETIME(),
        ReadyAtUtc = CASE WHEN @Step = 'ready' AND ReadyAtUtc IS NULL THEN SYSUTCDATETIME() ELSE ReadyAtUtc END,
        ReleasedAtUtc = CASE WHEN @Step = 'released' AND ReleasedAtUtc IS NULL THEN SYSUTCDATETIME() ELSE ReleasedAtUtc END
    WHERE Handle = @Handle;

    IF @Step = 'released'
        UPDATE vmp SET IsDeleted = 1
        FROM VMProperties vmp
        JOIN CodeProjectVmProvisioning p ON p.VMPropertyId = vmp.id
        WHERE p.Handle = @Handle;

    IF @Step = 'ready'
        UPDATE vmp SET Status = 7   -- VMState.running
        FROM VMProperties vmp
        JOIN CodeProjectVmProvisioning p ON p.VMPropertyId = vmp.id
        WHERE p.Handle = @Handle;
END
GO
