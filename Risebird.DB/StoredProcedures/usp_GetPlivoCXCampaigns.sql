-- =============================================
-- PlivoCX campaigns: the Campaigns list (/PlivoCXCampaign).
-- One row per campaign with its agent and the candidate states rolled up.
--   @Search : matches campaign name, IRF / list name, client or agent (NULL = all)
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPlivoCXCampaigns'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPlivoCXCampaigns
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetPlivoCXCampaigns]
    @Search NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Search = NULLIF(LTRIM(RTRIM(@Search)), '');

    SELECT
        pc.Id,
        pc.Name,
        pc.IRFId,
        pc.IRFName,
        pc.ClientName,
        pc.SourceType,
        pc.AgentId,
        a.Name                     AS AgentName,
        pc.CallPurpose,
        pc.[Status],
        pc.CreatedBy,
        pc.CreatedAt,
        ISNULL(s.Candidates, 0)    AS Candidates,
        ISNULL(s.Booked, 0)        AS Booked,
        ISNULL(s.NotBooked, 0)     AS NotBooked,
        ISNULL(s.Cold, 0)          AS Cold,
        ISNULL(s.Pending, 0)       AS Pending
    FROM      dbo.PlivoCXCampaign pc
    LEFT JOIN dbo.PlivoCXAgent    a ON a.Id = pc.AgentId
    OUTER APPLY (
        SELECT
            COUNT(*)                                                  AS Candidates,
            SUM(CASE WHEN cc.[State] = 'Booked'    THEN 1 ELSE 0 END) AS Booked,
            SUM(CASE WHEN cc.[State] = 'NotBooked' THEN 1 ELSE 0 END) AS NotBooked,
            SUM(CASE WHEN cc.[State] = 'Cold'      THEN 1 ELSE 0 END) AS Cold,
            SUM(CASE WHEN cc.[State] = 'Pending'   THEN 1 ELSE 0 END) AS Pending
        FROM dbo.PlivoCXCampaignCandidate cc
        WHERE cc.CampaignId = pc.Id
    ) s
    WHERE @Search IS NULL
       OR pc.Name       LIKE '%' + @Search + '%'
       OR pc.IRFName    LIKE '%' + @Search + '%'
       OR pc.ClientName LIKE '%' + @Search + '%'
       OR a.Name        LIKE '%' + @Search + '%'
    ORDER BY pc.Id DESC;
END
GO
