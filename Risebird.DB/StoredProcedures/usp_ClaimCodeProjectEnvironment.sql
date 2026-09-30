IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_ClaimCodeProjectEnvironment'))
BEGIN
    DROP PROCEDURE dbo.usp_ClaimCodeProjectEnvironment
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_ClaimCodeProjectEnvironment
    @SessionId varchar(50),
    @QuestionId int,
    @StackImageCode varchar(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Claimed bit = 0;
    BEGIN TRY
        INSERT INTO CodeProjectEnvironments (SessionId, QuestionId, StackImageCode, State)
        SELECT @SessionId, @QuestionId, @StackImageCode, 'requested'
        WHERE NOT EXISTS (SELECT 1 FROM CodeProjectEnvironments WITH (UPDLOCK, HOLDLOCK)
                          WHERE SessionId = @SessionId AND QuestionId = @QuestionId);
        IF @@ROWCOUNT = 1 SET @Claimed = 1;
    END TRY
    BEGIN CATCH
        -- Unique key race: someone else claimed it first.
        IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
    END CATCH
    SELECT @Claimed AS Claimed;
END
GO
