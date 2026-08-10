IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetVMIdByNameAndResourceGroup'))
BEGIN
    DROP PROCEDURE dbo.usp_GetVMIdByNameAndResourceGroup
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetVMIdByNameAndResourceGroup]
    @Name VARCHAR(200),
    @ResourceGroupName VARCHAR(200)
AS
BEGIN
    SELECT id
    FROM VMProperties
    WHERE Name = @Name AND ResourceGroupName = @ResourceGroupName
END
GO
