IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTransSkillGeneralByIRFId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransSkillGeneralByIRFId
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetTransSkillGeneralByIRFId]
    @IRFId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.TransSkillGeneral WHERE IRFId = @IRFId;
END
GO
