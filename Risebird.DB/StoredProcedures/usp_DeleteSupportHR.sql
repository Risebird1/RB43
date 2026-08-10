IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteSupportHR'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteSupportHR
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_DeleteSupportHR]
    @id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM Support
    WHERE id = @id
END
GO
