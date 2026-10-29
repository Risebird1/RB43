IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewProfitabilityFilterOptions'))
    DROP PROCEDURE dbo.usp_GetInterviewProfitabilityFilterOptions
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Dropdown options for the Interview Profitability filters.
--   1. Clients.
--   2. The IRFs of @ClientId, newest first (empty when no client is chosen, to keep the page light).
CREATE PROCEDURE usp_GetInterviewProfitabilityFilterOptions
    @ClientId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT c.id AS Id, c.Name AS Name
    FROM Client c
    WHERE ISNULL(c.Name, '') <> ''
    ORDER BY c.Name;

    SELECT irf.id AS Id,
           irf.IRFId AS Code,
           irf.JDTiltle AS Title
    FROM IRF irf
    WHERE @ClientId IS NOT NULL
      AND irf.ClientId = @ClientId
    ORDER BY irf.id DESC;
END
GO
