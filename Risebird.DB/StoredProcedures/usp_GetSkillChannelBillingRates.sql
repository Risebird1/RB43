IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSkillChannelBillingRates'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSkillChannelBillingRates
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ============================================================
-- usp_GetSkillChannelBillingRates
-- Returns all active skill channels for clients where
-- InterviewerBillingType = 2 (Skill Channel billing).
-- BillingRate shows the custom rate if set, otherwise falls
-- back to Client.PerInterviewRate as the default.
-- ============================================================
CREATE PROCEDURE [dbo].[usp_GetSkillChannelBillingRates]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        irf.id                                                              AS IRFAutoId,
        irf.IRFId                                                           AS IRFBusinessKey,
        cl.id                                                               AS ClientId,
        cl.Name                                                             AS ClientName,
        CASE
            WHEN irf.SkillSetsevaluate > 1 AND ISNULL(irf.JDTitleTwo, '') != ''
            THEN irf.JDTiltle + ' and ' + irf.JDTitleTwo
            ELSE irf.JDTiltle
        END                                                                 AS JDTitle,
        irf.JobCountry,
        irf.CompanyType,
        irf.Heuristicsscore,
        ISNULL(cl.PerInterviewRate, 0)                                      AS DefaultRate,
        ISNULL(scbr.BillingRate, cl.PerInterviewRate)                       AS BillingRate,
        CASE WHEN scbr.id IS NOT NULL THEN 1 ELSE 0 END                     AS IsRateSet,
        ISNULL(cl.BillingCurrency, 'INR')                                   AS BillingCurrency,
        (SELECT COUNT(id) FROM TransInterviewer WHERE IRFId = irf.id)       AS TotalInterviewers,
        (SELECT COUNT(id) FROM TransInterviewer WHERE IRFId = irf.id AND IsActive = 1) AS ActiveInterviewers
    FROM IRF irf
    INNER JOIN Client cl ON cl.id = irf.ClientId
    LEFT  JOIN SkillChannelBillingRate scbr ON scbr.IRFId = irf.id
    WHERE ISNULL(cl.InterviewerBillingType, 1) = 2
      AND ISNULL(irf.IsDeleted, 0) = 0
      AND ISNULL(irf.IsPaused,  0) = 0 AND DateAdd(month,-1,Getdate())<IRF.EndDate
    ORDER BY cl.Name, irf.JDTiltle;
END
GO
