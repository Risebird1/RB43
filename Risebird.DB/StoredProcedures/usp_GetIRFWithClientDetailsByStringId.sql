IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFWithClientDetailsByStringId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFWithClientDetailsByStringId
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetIRFWithClientDetailsByStringId]
    @IRFId VARCHAR(200)
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
    WHERE IRF.IRFId = @IRFId;
END
GO
