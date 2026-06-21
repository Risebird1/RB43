IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetUniqueIdByEmailId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUniqueIdByEmailId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetUniqueIdByEmailId]
    @emailid VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT uniqueid
    FROM UserList
    WHERE EmailId = @emailid
END
GO
