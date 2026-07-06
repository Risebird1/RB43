IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTransJoinabilityByIRFIdWithMatrix'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransJoinabilityByIRFIdWithMatrix
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetTransJoinabilityByIRFIdWithMatrix]
    @IRFId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT tj.*, jm.Name AS Name
    FROM dbo.TransJoinability tj
    INNER JOIN dbo.JoinabilityMatrix jm ON tj.type = jm.Id
    WHERE tj.IRFId = @IRFId;
END
GO
