IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTransSkillGeneralByIRFIdWithMatrix'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransSkillGeneralByIRFIdWithMatrix
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetTransSkillGeneralByIRFIdWithMatrix]
    @IRFId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT tsg.*, gsm.Name AS Name
    FROM dbo.TransSkillGeneral tsg
    INNER JOIN dbo.GeneralSkillMatrix gsm ON tsg.type = gsm.Id
    WHERE tsg.IRFId = @IRFId;
END
GO
