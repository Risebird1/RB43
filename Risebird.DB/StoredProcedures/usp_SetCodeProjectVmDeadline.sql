IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SetCodeProjectVmDeadline'))
BEGIN
    DROP PROCEDURE dbo.usp_SetCodeProjectVmDeadline
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_SetCodeProjectVmDeadline
    @Handle varchar(64),
    @DeadlineUtc datetime2
AS
BEGIN
    SET NOCOUNT ON;
    -- Set once: a second handover call (reconnect) must not extend the slot.
    UPDATE CodeProjectVmProvisioning SET DeadlineUtc = @DeadlineUtc, UpdatedAtUtc = SYSUTCDATETIME()
    WHERE Handle = @Handle AND DeadlineUtc IS NULL;
END
GO
