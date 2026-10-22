IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetReadyCodeProjectVms'))
BEGIN
    DROP PROCEDURE dbo.usp_GetReadyCodeProjectVms
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetReadyCodeProjectVms
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Handle, VmName, ResourceGroupName, CandidateUserName, DeadlineUtc, LockedAtUtc, SeenSessionIdsJson, IntegrityFlagsJson
    FROM CodeProjectVmProvisioning
    WHERE Step = 'ready';
END
GO
