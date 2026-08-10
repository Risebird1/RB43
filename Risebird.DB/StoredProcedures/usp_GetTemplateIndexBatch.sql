IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTemplateIndexBatch'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTemplateIndexBatch
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns denormalized ClonableSkillChannel (template) rows for Azure AI Search index (templateindex).
-- Based on usp_GetAllClonableSkillChannels.
-- @ClonableSkillChannelId NULL = active rows only (FullCrawl). Soft-deleted rows are excluded here and
--   removed from the index individually via UpsertAsync -> ShouldRemoveFromIndex when they are set deleted.
-- @ClonableSkillChannelId set  = single row by csc.id, including soft-deleted (Upsert after delete/restore).
CREATE PROCEDURE [dbo].[usp_GetTemplateIndexBatch]
    @ClonableSkillChannelId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        csc.id                                     AS ClonableSkillChannelId,
        csc.Name,
        csc.CreatedFromIRFId,
        csc.MeetingPlatform,
        ir.CompanyType                              AS CompanyTypeId,
        ir.SeniorityLevelId,
        ir.CountryType,
        asg.AssessmentGroup                         AS AssessmentGroupName,
        asg.id                                       AS AssessmentGroupId,
        ir.DifficultyLevel,
        ir.EmploymentType,
        ir.PerHourRate,
        agc.CategoryName,
        COUNT(IRF.IRFId)                            AS SkillChannelCounts,
        ir.PrimarySkills,
        csc.CreatedOn,
        ir.Clients                                  AS TaggedClients,
        CAST(ISNULL(csc.IsDeleted, 0) AS BIT)       AS IsDeleted,
        irfc.IRFID                                  AS SkillChannelName,
        ulir.EmailId                                AS ChannelOwner
    FROM       dbo.ClonableSkillChannel csc
    LEFT JOIN  dbo.IRFRecommendations ir ON ir.CloneIRFId = csc.id
    JOIN       dbo.IRF irfc ON irfc.id = csc.CreatedFromIRFId
    JOIN       dbo.AssessmentGroup asg ON asg.id = irfc.AssessmentGroupId
    LEFT JOIN  dbo.AssessmentGroupCategory agc ON agc.id = asg.Categoryid
    LEFT JOIN  dbo.IRF ON IRF.CreatedFromTemplateId = csc.id
    LEFT JOIN  dbo.Userlist ulir ON ulir.id = irfc.RisebirdHr
    WHERE (@ClonableSkillChannelId IS NULL AND ISNULL(csc.IsDeleted, 0) = 0)
       OR (@ClonableSkillChannelId IS NOT NULL AND csc.id = @ClonableSkillChannelId)
    GROUP BY
        csc.id, csc.Name, csc.CreatedFromIRFId, csc.MeetingPlatform, ir.CompanyType,
        ir.SeniorityLevelId, ir.CountryType, asg.AssessmentGroup, asg.id,
        ir.DifficultyLevel, ir.EmploymentType, ir.PerHourRate, agc.CategoryName,
        ir.PrimarySkills, csc.CreatedOn, ir.Clients, csc.IsDeleted, irfc.IRFID, ulir.EmailId
    ORDER BY csc.CreatedOn DESC;
END
GO
