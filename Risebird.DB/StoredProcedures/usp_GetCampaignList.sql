
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCampaignList'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetCampaignList]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_GetCampaignList]


AS
Begin

select agc.*, ag.AssessmentGroup as AssessmentGroupName from AGCampaigns agc
join AssessmentGroup ag on ag.id = agc.AssessmentGroupId
order by agc.CreatedAt desc

End



GO

