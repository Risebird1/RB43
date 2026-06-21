

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientDetailsByIRFId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientDetailsByIRFId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetClientDetailsByIRFId]
    @IRFId VARCHAR(255)
AS
BEGIN
    SELECT cl.IsQCRequired, cl.IsRedoInterview
    FROM Client cl
    JOIN IRF ON IRF.ClientId = cl.id
    WHERE IRF.IRFId = @IRFId
END
GO
