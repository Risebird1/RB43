IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFWithClientDetailsById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFWithClientDetailsById
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetIRFWithClientDetailsById]
    @id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        IRF.*,
        Client.[Name]      AS ClientCompName,
        Client.GST         AS ClientGST,
        Client.OfficeAddress AS ClientAddress
    FROM dbo.IRF
    INNER JOIN dbo.Client ON IRF.ClientId = Client.id
    WHERE IRF.id = @id;
END
GO
