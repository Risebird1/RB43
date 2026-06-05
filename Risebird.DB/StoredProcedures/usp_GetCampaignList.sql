
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
BEGIN
    SELECT
        agc.*,
        ag.AssessmentGroup AS AssessmentGroupName,
        bcl.Id              AS BatchCallLogId,
        bcl.InitiatedAt     AS BatchCallInitiatedAt,
        bcl.InterviewersAttempted  AS BatchCallAttempted,
        bcl.InterviewersSucceeded  AS BatchCallSucceeded,
        bcl.InterviewersFailed     AS BatchCallFailed,
        bcl.InterviewersSkipped    AS BatchCallSkipped,
        bcl.CompletedAt     AS BatchCallCompletedAt,
        bcl.Status          AS BatchCallStatus
    FROM AGCampaigns agc
    INNER JOIN AssessmentGroup ag ON ag.id = agc.AssessmentGroupId
    OUTER APPLY (
        SELECT TOP 1 *
        FROM IABatchCallLog l
        WHERE l.CampaignId = agc.id
        ORDER BY l.Id DESC
    ) bcl
    ORDER BY agc.CreatedAt DESC
END



GO

