IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSelectabilityMatrix'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSelectabilityMatrix
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetSelectabilityMatrix]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM SelectabilityMatrix
END
GO
