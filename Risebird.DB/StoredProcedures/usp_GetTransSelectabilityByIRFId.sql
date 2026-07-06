IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTransSelectabilityByIRFId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransSelectabilityByIRFId
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetTransSelectabilityByIRFId]
    @IRFId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.TransSelectability WHERE IRFId = @IRFId;
END
GO
