IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTransSelectabilityByIRFIdWithMatrix'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransSelectabilityByIRFIdWithMatrix
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetTransSelectabilityByIRFIdWithMatrix]
    @IRFId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ts.*, sm.Name AS Name
    FROM dbo.TransSelectability ts
    INNER JOIN dbo.SelectabilityMatrix sm ON ts.type = sm.id
    WHERE ts.IRFId = @IRFId;
END
GO
