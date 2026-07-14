IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetDriveIRFMetadata'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDriveIRFMetadata
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetDriveIRFMetadata]
    @irfIds varchar(max)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        irf.id,
        irf.IRFId,
        irf.ClientName,
        irf.JDTiltle,
        cl.[Name] AS ClientCompName,
        LTRIM(RTRIM(irf.AssessmentGroupId)) AS AssessmentGroupId,
        ag.AssessmentGroup AS AssessmentGroupName
    FROM dbo.IRF irf
    LEFT JOIN dbo.Client cl ON irf.ClientId = cl.id
    LEFT JOIN dbo.AssessmentGroup ag
        ON ag.id = TRY_CAST(NULLIF(LTRIM(RTRIM(irf.AssessmentGroupId)), '') AS INT)
    WHERE irf.IsDeleted = 0
      AND @irfIds IS NOT NULL
      AND LTRIM(RTRIM(@irfIds)) <> ''
      AND (
          LTRIM(RTRIM(irf.IRFId)) IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@irfIds, ','))
          OR CAST(irf.id AS varchar(20)) IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@irfIds, ','))
      );
END
GO
