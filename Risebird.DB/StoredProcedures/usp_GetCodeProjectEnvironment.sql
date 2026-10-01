IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCodeProjectEnvironment'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCodeProjectEnvironment
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetCodeProjectEnvironment
    @SessionId varchar(50),
    @QuestionId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT id, SessionId, QuestionId, StackImageCode, State, EnvironmentHandle, ConnectUrl, VMPropertyId,
           Message, RequestedAtUtc, ReadyAtUtc, HandedOverAtUtc, SubmittedAtUtc
    FROM CodeProjectEnvironments
    WHERE SessionId = @SessionId AND QuestionId = @QuestionId;
END
GO
