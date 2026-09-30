IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateCodeProjectValidation'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateCodeProjectValidation
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_UpdateCodeProjectValidation
    @QuestionId int,
    @ValidationStatus varchar(20),
    @ValidationJson nvarchar(max) = NULL,
    @ProjectBlobUri varchar(500) = NULL,
    @SecretsBlobUri varchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CodeProjectDetails
    SET ValidationStatus = @ValidationStatus,
        ValidationJson   = @ValidationJson,
        ProjectBlobUri   = ISNULL(@ProjectBlobUri, ProjectBlobUri),
        SecretsBlobUri   = ISNULL(@SecretsBlobUri, SecretsBlobUri),
        ValidatedAtUtc   = CASE WHEN @ValidationStatus IN ('passed','failed') THEN SYSUTCDATETIME() ELSE ValidatedAtUtc END
    WHERE QuestionId = @QuestionId;
    SELECT @@ROWCOUNT AS Affected;
END
GO
