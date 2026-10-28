IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetLiveCodeProjectVmHandles'))
BEGIN
    DROP PROCEDURE dbo.usp_GetLiveCodeProjectVmHandles
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetLiveCodeProjectVmHandles
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Handle FROM CodeProjectVmProvisioning WHERE Step <> 'released';
END
GO
