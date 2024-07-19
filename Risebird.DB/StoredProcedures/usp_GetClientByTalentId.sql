IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetClientByTalentId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientByTalentId
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetClientByTalentId
@tenantId varchar(50)
AS
BEGIN
Select * From Client where tenantId=@tenantId
END
GO