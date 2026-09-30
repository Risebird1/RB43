IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_StartCodeProjectInfraSetup'))
BEGIN
    DROP PROCEDURE dbo.usp_StartCodeProjectInfraSetup
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_StartCodeProjectInfraSetup
    @StartedBy varchar(150) = NULL,
    @InitialStateJson nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id int = (SELECT TOP 1 Id FROM CodeProjectInfraSetupRuns WITH (UPDLOCK, HOLDLOCK)
                       WHERE Status IN ('running', 'failed', 'blocked') ORDER BY Id DESC);
    IF @Id IS NOT NULL
    BEGIN
        UPDATE CodeProjectInfraSetupRuns SET Status = 'running', UpdatedAtUtc = SYSUTCDATETIME(), LeaseUntilUtc = NULL WHERE Id = @Id;
        SELECT @Id AS Id;
        RETURN;
    END
    INSERT INTO CodeProjectInfraSetupRuns (Status, StateJson, StartedBy) VALUES ('running', @InitialStateJson, @StartedBy);
    SELECT CAST(SCOPE_IDENTITY() AS int) AS Id;
END
GO
