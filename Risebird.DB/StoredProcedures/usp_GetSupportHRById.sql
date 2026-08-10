IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSupportHRById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSupportHRById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetSupportHRById]
    @id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM Support
    WHERE id = @id
END
GO
