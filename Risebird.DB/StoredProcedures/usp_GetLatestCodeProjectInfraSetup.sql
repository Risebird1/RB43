IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetLatestCodeProjectInfraSetup'))
BEGIN
    DROP PROCEDURE dbo.usp_GetLatestCodeProjectInfraSetup
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetLatestCodeProjectInfraSetup
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 Id, Status, StateJson, StartedBy, StartedAtUtc, UpdatedAtUtc, CompletedAtUtc
    FROM CodeProjectInfraSetupRuns ORDER BY Id DESC;
END
GO
