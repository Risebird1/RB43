IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetVMPropertiesById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetVMPropertiesById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetVMPropertiesById]
    @id INT
AS
BEGIN
    SELECT * FROM VMProperties WHERE id = @id
END
GO
