IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetGeneralSkillMatrix'))
BEGIN
    DROP PROCEDURE dbo.usp_GetGeneralSkillMatrix
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetGeneralSkillMatrix]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM GeneralSkillMatrix
END
GO
