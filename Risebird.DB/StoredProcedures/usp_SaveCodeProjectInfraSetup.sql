IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveCodeProjectInfraSetup'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveCodeProjectInfraSetup
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_SaveCodeProjectInfraSetup
    @Id int,
    @Status varchar(20),
    @StateJson nvarchar(max),
    @ReleaseLease bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CodeProjectInfraSetupRuns
    SET Status = @Status,
        StateJson = @StateJson,
        UpdatedAtUtc = SYSUTCDATETIME(),
        CompletedAtUtc = CASE WHEN @Status = 'completed' THEN SYSUTCDATETIME() ELSE CompletedAtUtc END,
        LeaseUntilUtc = CASE WHEN @ReleaseLease = 1 THEN NULL ELSE LeaseUntilUtc END
    WHERE Id = @Id;
END
GO
