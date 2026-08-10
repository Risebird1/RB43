IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetJoinabilityMatrix'))
BEGIN
    DROP PROCEDURE dbo.usp_GetJoinabilityMatrix
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetJoinabilityMatrix]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM JoinabilityMatrix
END
GO
