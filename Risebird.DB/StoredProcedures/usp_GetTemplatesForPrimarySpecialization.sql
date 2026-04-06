IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTemplatesForPrimarySpecialization'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTemplatesForPrimarySpecialization
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Returns clonable templates (skill channel templates) whose assessment group names
-- best match the provided primary specialization text.
-- If no matches found, returns ALL active templates.
-- Uses word overlap matching similar to usp_GetSimilarAssessmentGroups.
CREATE PROCEDURE [dbo].[usp_GetTemplatesForPrimarySpecialization]
    @PrimarySpecialization NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    -- Table to hold search tokens
    DECLARE @SearchTokens TABLE (Token NVARCHAR(100));
    
    -- Split the primary specialization into tokens
    INSERT INTO @SearchTokens (Token)
    SELECT LTRIM(RTRIM(value))
    FROM STRING_SPLIT(ISNULL(@PrimarySpecialization, ''), ' ')
    WHERE LTRIM(RTRIM(value)) <> '' AND LEN(LTRIM(RTRIM(value))) > 2;

    -- Table to hold matching assessment groups with match score
    DECLARE @MatchingAGs TABLE (
        AssessmentGroupId INT,
        AssessmentGroupName NVARCHAR(200),
        MatchScore INT
    );

    -- Calculate match score for each assessment group (only if we have tokens)
    IF EXISTS (SELECT 1 FROM @SearchTokens)
    BEGIN
        INSERT INTO @MatchingAGs (AssessmentGroupId, AssessmentGroupName, MatchScore)
        SELECT 
            ag.id,
            ag.AssessmentGroup,
            (
                SELECT COUNT(*)
                FROM @SearchTokens st
                WHERE ag.AssessmentGroup LIKE '%' + st.Token + '%'
            ) AS MatchScore
        FROM AssessmentGroup ag
        WHERE EXISTS (
            SELECT 1 FROM @SearchTokens st
            WHERE ag.AssessmentGroup LIKE '%' + st.Token + '%'
        );
    END

    -- If we have matching templates, return them first ordered by score
    -- Then append all other templates with MatchScore = 0
    SELECT DISTINCT
        csc.id AS TemplateId,
        csc.Name AS TemplateName,
        REPLACE(REPLACE(REPLACE(REPLACE(csc.Name, '<b>', ''), '</b>', ''), '<strong>', ''), '</strong>', '') AS TemplateNameClean,
        asg.id AS AssessmentGroupId,
        asg.AssessmentGroup AS AssessmentGroupName,
        csc.MeetingPlatform,
        irfc.IRFId AS SourceIRFStringId,
        csc.CreatedFromIRFId AS SourceIRFId,
        ulir.EmailId AS ChannelOwner,
        ISNULL(mag.MatchScore, 0) AS MatchScore,
        (SELECT COUNT(*) FROM IRF WHERE IRF.CreatedFromTemplateId = csc.id) AS SkillChannelCounts
    FROM ClonableSkillChannel csc
    INNER JOIN IRF irfc ON irfc.id = csc.CreatedFromIRFId
    INNER JOIN AssessmentGroup asg ON asg.id = irfc.AssessmentGroupId
    LEFT JOIN @MatchingAGs mag ON mag.AssessmentGroupId = asg.id
    LEFT JOIN UserList ulir ON ulir.id = irfc.RisebirdHr
    WHERE (csc.IsDeleted IS NULL OR csc.IsDeleted = 0)
    ORDER BY ISNULL(mag.MatchScore, 0) DESC, asg.AssessmentGroup ASC, csc.Name ASC;
END
GO
