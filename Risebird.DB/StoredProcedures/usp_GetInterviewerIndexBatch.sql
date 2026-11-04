IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerIndexBatch'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerIndexBatch
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns denormalized interviewer rows for Azure AI Search index (interviewerindex).
-- @InterviewerProfileId NULL + @UserId NULL = all active (FullCrawl)
-- Either id set = single row including deleted (Upsert / Remove)
CREATE PROCEDURE [dbo].[usp_GetInterviewerIndexBatch]
    @InterviewerProfileId INT = NULL,
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        intp.Id                                              AS InterviewerProfileId,
        intp.UserId,
        ul.FirstName,
        ul.LastName,
        intp.NickName,
        ul.EmailId,
        ul.MobileNumber,
        PrimarySkill = STUFF((
            SELECT DISTINCT ', ' + ski.Name
            FROM dbo.TransInterviewerSkill tis
            INNER JOIN dbo.Skill ski ON ski.id = tis.SkillId
            WHERE tis.InterviewerProfileId = intp.Id
            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, ''),
        intp.AssessmentGroupId,
        AssessmentGroupNames = STUFF((
            SELECT ', ' + ag.AssessmentGroup
            FROM STRING_SPLIT(ISNULL(intp.AssessmentGroupId, ''), ',') s
            INNER JOIN dbo.AssessmentGroup ag ON ag.id = TRY_CAST(LTRIM(RTRIM(s.value)) AS INT)
            WHERE LTRIM(RTRIM(s.value)) <> ''
            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, ''),
        CAST(irc.Senioritylevel AS VARCHAR(100))             AS Senioritylevel,
        CAST(irc.PerHourRateIndia AS VARCHAR(100))           AS PerHourRateIndia,
        CAST(irc.PerHourRateOutside AS VARCHAR(100))         AS PerHourRateOutside,
        CAST(irc.CompentencyLevel AS VARCHAR(100))           AS CompentencyLevel,
        CAST(irc.ProfileType AS VARCHAR(100))                AS ProfileType,
        irc.CompanyType,
        COALESCE(intp.Country, irc.Country)                  AS Country,
        ISNULL(intp.IsLeadInterviewer, 0)                    AS IsLeadInterviewer,
        ISNULL(ul.IsActive, 0)                               AS IsActive,
        ISNULL(ul.IsInterviewerAccess, 0)                    AS IsInterviewerAccess,
        ISNULL(intp.IsApproved, 0)                           AS IsApproved,
        ISNULL(intp.IsInterest, 0)                           AS IsInterested,
        ISNULL(intp.IsDeleted, 0)                            AS IsDeleted,
        ISNULL(intp.Isavailable, 0)                          AS Isavailable,
        AvailabilityStatusValue = CASE
            WHEN intp.Isavailable = 1 THEN 'Available'
            WHEN intp.Isavailable = 2 THEN 'Not Available'
            WHEN intp.Isavailable = 3 THEN 'Need Clarity'
            WHEN intp.Isavailable = 4 THEN 'Drop My Profile'
            WHEN intp.Isavailable = 5 THEN 'Opted out of calls'
            ELSE 'no data'
        END,
        ISNULL(intp.VideoConfirmation, 0)                    AS VideoConfirmation,
        intp.ResumeLink,
        intp.CodedResumeLink,
        intp.City,
        intp.CurrentRole,
        ISNULL(intp.Experience, 0)                           AS Experience,
        intp.PreferredDomain,
        ul.uniqueid,
        intp.AvailabilityResponseDate,
        ISNULL(intp.IsHoldPayment, 0)                        AS IsHoldPayment
    FROM       dbo.InterviewerProfile intp
    INNER JOIN dbo.UserList ul ON ul.id = intp.UserId
    LEFT JOIN  dbo.InterviewerRecommendation irc ON irc.Userid = intp.UserId
    WHERE (@InterviewerProfileId IS NULL OR intp.Id = @InterviewerProfileId)
      AND (@UserId IS NULL OR intp.UserId = @UserId)
      AND (
            @InterviewerProfileId IS NOT NULL
            OR @UserId IS NOT NULL
            OR ISNULL(intp.IsDeleted, 0) = 0
          );
END
GO
