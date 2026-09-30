IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateCodeProjectEnvironment'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateCodeProjectEnvironment
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_UpdateCodeProjectEnvironment
    @SessionId varchar(50),
    @QuestionId int,
    @State varchar(20),
    @EnvironmentHandle varchar(200) = NULL,
    @ConnectUrl varchar(1000) = NULL,
    @VMPropertyId int = NULL,
    @Message nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CodeProjectEnvironments
    SET State = @State,
        EnvironmentHandle = ISNULL(@EnvironmentHandle, EnvironmentHandle),
        ConnectUrl = ISNULL(@ConnectUrl, ConnectUrl),
        VMPropertyId = ISNULL(@VMPropertyId, VMPropertyId),
        Message = @Message,
        ReadyAtUtc = CASE WHEN @State = 'ready' AND ReadyAtUtc IS NULL THEN SYSUTCDATETIME() ELSE ReadyAtUtc END,
        -- Set once: a reconnect must not restart the candidate's clock.
        HandedOverAtUtc = CASE WHEN @State = 'handedover' AND HandedOverAtUtc IS NULL THEN SYSUTCDATETIME() ELSE HandedOverAtUtc END,
        SubmittedAtUtc = CASE WHEN @State = 'submitted' AND SubmittedAtUtc IS NULL THEN SYSUTCDATETIME() ELSE SubmittedAtUtc END
    WHERE SessionId = @SessionId AND QuestionId = @QuestionId;
    SELECT @@ROWCOUNT AS Affected;
END
GO
