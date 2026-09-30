IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCodeProjectSettings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCodeProjectSettings
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetCodeProjectSettings
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Name, Value, IsSecret FROM CodeProjectSettings;
END
GO
