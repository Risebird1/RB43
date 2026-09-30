IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetUnfinishedCodeProjectValidations'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUnfinishedCodeProjectValidations
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetUnfinishedCodeProjectValidations
AS
BEGIN
    SET NOCOUNT ON;
    SELECT cpd.QuestionId
    FROM CodeProjectDetails cpd
    WHERE cpd.ValidationStatus IN ('pending','validating');
END
GO
