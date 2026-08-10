IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTransJoinabilityByIRFId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransJoinabilityByIRFId
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetTransJoinabilityByIRFId]
    @IRFId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.TransJoinability WHERE IRFId = @IRFId;
END
GO
