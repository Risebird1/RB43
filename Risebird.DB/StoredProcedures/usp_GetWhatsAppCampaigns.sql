-- WhatsApp campaigns: the Campaigns list (/WhatsAppCampaign) - every campaign with its candidate
-- states rolled up, newest first, optionally filtered by name / IRF / client.
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetWhatsAppCampaigns'))
BEGIN
    DROP PROCEDURE dbo.usp_GetWhatsAppCampaigns
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetWhatsAppCampaigns]
    @Search NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Search = NULLIF(LTRIM(RTRIM(@Search)), '');

    SELECT
        wc.Id,
        wc.Name,
        wc.SourceType,
        wc.Purpose,
        wc.EventName,
        wc.IRFId,
        wc.IRFName,
        wc.ClientName,
        wc.[Status],
        wc.CreatedBy,
        wc.CreatedAt,
        ISNULL(s.Candidates, 0) AS Candidates,
        ISNULL(s.Sent, 0)       AS Sent,
        ISNULL(s.Converted, 0)  AS Converted,
        ISNULL(s.Declined, 0)   AS Declined,
        ISNULL(s.NoResponse, 0) AS NoResponse,
        ISNULL(s.NoNumber, 0)   AS NoNumber,
        ISNULL(s.Failed, 0)     AS Failed,
        ISNULL(s.Pending, 0)    AS Pending
    FROM dbo.WhatsAppCampaign wc
    OUTER APPLY (
        SELECT
            COUNT(*)                                                   AS Candidates,
            SUM(CASE WHEN cc.[State] = 'Sent'       THEN 1 ELSE 0 END) AS Sent,
            SUM(CASE WHEN cc.[State] = 'Converted'  THEN 1 ELSE 0 END) AS Converted,
            SUM(CASE WHEN cc.[State] = 'Declined'   THEN 1 ELSE 0 END) AS Declined,
            SUM(CASE WHEN cc.[State] = 'NoResponse' THEN 1 ELSE 0 END) AS NoResponse,
            SUM(CASE WHEN cc.[State] = 'NoNumber'   THEN 1 ELSE 0 END) AS NoNumber,
            SUM(CASE WHEN cc.[State] = 'Failed'     THEN 1 ELSE 0 END) AS Failed,
            SUM(CASE WHEN cc.[State] = 'Pending'    THEN 1 ELSE 0 END) AS Pending
        FROM dbo.WhatsAppCampaignCandidate cc
        WHERE cc.CampaignId = wc.Id
    ) s
    WHERE @Search IS NULL
       OR wc.Name       LIKE '%' + @Search + '%'
       OR wc.IRFName    LIKE '%' + @Search + '%'
       OR wc.ClientName LIKE '%' + @Search + '%'
    ORDER BY wc.Id DESC;
END
GO
