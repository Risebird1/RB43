IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIrfIndexBatch'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIrfIndexBatch
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns denormalized IRF rows for Azure AI Search index (irfindex).
-- @IrfIntId NULL  = all active IRFs (FullCrawl)
-- @IrfIntId set   = single IRF including deleted (Upsert / Remove)
CREATE PROCEDURE [dbo].[usp_GetIrfIndexBatch]
    @IrfIntId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH ConfirmedMeetings AS (
        SELECT
            irf.id AS IrfIntId,
            COUNT(*) AS ConfirmedMeetings
        FROM       dbo.InterviewMeetings im
        INNER JOIN dbo.IRF              irf ON irf.IRFId = im.IRFId
        INNER JOIN dbo.UserList         ul  ON ul.EmailId = im.candidateEmail
        INNER JOIN dbo.CandidatesMaster cm  ON cm.UserId = ul.id
        INNER JOIN dbo.Candidate        ca  ON ca.CandidateMasterId = cm.id AND ca.IRFId = irf.id
        WHERE im.confirmationStatus = '1'
        GROUP BY irf.id
    ),
    TotalMeetings AS (
        SELECT ca.IRFId AS IrfIntId, COUNT(*) AS TotalMeetings
        FROM dbo.Candidate ca
        GROUP BY ca.IRFId
    ),
    SelectedMeetings AS (
        SELECT
            irf.id AS IrfIntId,
            COUNT(DISTINCT im.id) AS SelectedMeetings
        FROM       dbo.IRF irf
        LEFT JOIN  dbo.InterviewMeetings im ON irf.IRFId = im.IRFId
        LEFT JOIN  dbo.InterviewComments ic ON im.id = ic.InterviewId
        INNER JOIN dbo.UserList         ul  ON ul.EmailId = im.candidateEmail
        INNER JOIN dbo.CandidatesMaster cm  ON cm.UserId = ul.id
        INNER JOIN dbo.Candidate        ca  ON ca.CandidateMasterId = cm.id AND ca.IRFId = irf.id
        WHERE ic.InterviewStatus IN (2, 4)
        GROUP BY irf.id
    ),
    ScheduledMeetings AS (
        SELECT
            irf.id AS IrfIntId,
            COUNT(DISTINCT im.id) AS ScheduledMeetings
        FROM       dbo.IRF irf
        LEFT JOIN  dbo.InterviewMeetings im ON irf.IRFId = im.IRFId
        LEFT JOIN  dbo.InterviewComments ic ON ic.InterviewId = im.id
        INNER JOIN dbo.UserList         ul  ON ul.EmailId = im.candidateEmail
        INNER JOIN dbo.CandidatesMaster cm  ON cm.UserId = ul.id
        INNER JOIN dbo.Candidate        ca  ON ca.CandidateMasterId = cm.id AND ca.IRFId = irf.id
        WHERE ic.MeetingStatus = 4 AND im.confirmationStatus = '1'
        GROUP BY irf.id
    ),
    ViewerEmails AS (
        SELECT
            v.IrfIntId,
            STRING_AGG(v.EmailId, '|') WITHIN GROUP (ORDER BY v.EmailId) AS AllowedViewerEmails
        FROM (
            SELECT DISTINCT irf.id AS IrfIntId, ul.EmailId
            FROM dbo.IRF irf
            INNER JOIN dbo.TransClientUser tc ON tc.IRFId = irf.id
            INNER JOIN dbo.UserList ul ON ul.id = tc.ClientUserId
            WHERE ul.EmailId IS NOT NULL

            UNION

            SELECT DISTINCT irf.id, ul.EmailId
            FROM dbo.IRF irf
            INNER JOIN dbo.TransRecruiter tr ON tr.IRFId = irf.id
            INNER JOIN dbo.UserList ul ON ul.id = tr.UserId
            WHERE ul.EmailId IS NOT NULL

            UNION

            SELECT DISTINCT irf.id, ulh.EmailId
            FROM dbo.IRF irf
            INNER JOIN dbo.UserList ulh ON ulh.id = irf.RisebirdHr
            WHERE ulh.EmailId IS NOT NULL

            UNION

            SELECT DISTINCT irf.id, ulsh.EmailId
            FROM dbo.IRF irf
            INNER JOIN dbo.SeniorHr sh ON sh.ClientId = irf.ClientId
            INNER JOIN dbo.UserList ulsh ON ulsh.id = sh.UserId
            WHERE ulsh.EmailId IS NOT NULL

            UNION

            SELECT DISTINCT irf.id, ulhr.EmailId
            FROM dbo.IRF irf
            INNER JOIN dbo.TransHR th ON th.IRFId = irf.id
            INNER JOIN dbo.HR hr ON hr.id = th.HRId
            INNER JOIN dbo.UserList ulhr ON ulhr.id = hr.UserId
            WHERE ulhr.EmailId IS NOT NULL
        ) v
        GROUP BY v.IrfIntId
    )
    SELECT
        irf.id                                           AS IrfIntId,
        irf.IRFId,
        irf.JDTiltle,
        PrimarySkill = CASE
            WHEN CHARINDEX('|', irf.JDTiltle) > 0
            THEN LTRIM(RTRIM(SUBSTRING(irf.JDTiltle, 1, CHARINDEX('|', irf.JDTiltle) - 1)))
            ELSE LTRIM(RTRIM(irf.JDTiltle))
        END,
        cl.[Name]                                        AS ClientCompName,
        irf.ClientName,
        irf.StartDate,
        irf.EndDate,
        irf.JDLink,
        irf.RisebirdHr,
        ulh.EmailId                                      AS RisebirdHREmail,
        irf.PerMintuteRate,
        ISNULL(irf.IsDeleted, 0)                         AS IsDeleted,
        ISNULL(irf.IsPaused, 0)                          AS IsPaused,
        -- Profile-dependent UI flag; always false in index, set from profileName on read.
        CAST(0 AS BIT)                                   AS IsClientUser,
        irf.TotalInvites,
        irf.TotalBudget,
        TotalBudgetConsumed = CAST(ISNULL(irf.PerMintuteRate, 0) * ISNULL(cm.ConfirmedMeetings, 0) AS FLOAT),
        irf.CalendlyLink,
        irf.CompanyDetails,
        ISNULL(irf.Discount, 0)                          AS Discount,
        ISNULL(irf.IsBiometricEnabled, 0)                AS IsBiometricEnabled,
        irf.PriorityLevel,
        cm.ConfirmedMeetings,
        tm.TotalMeetings,
        sm.SelectedMeetings,
        sch.ScheduledMeetings,
        ISNULL(irf.flagcount, 0)                         AS FlagCount,
        irf.CreatedFromTemplateId,
        irf.AssessmentGroupId,
        ISNULL(irf.IsAIInterview, 0)                     AS IsAIInterview,
        ISNULL(irf.AIInterviewType, 0)                   AS AIInterviewType,
        ISNULL(irf.InterviewerBillingCycle, 0)           AS InterviewerBillingCycle,
        ISNULL(irf.CreatedOn, irf.StartDate)             AS CreatedOn,
        CAST(1 AS BIT)                                   AS AllowAllInternal,
        ve.AllowedViewerEmails,
        ISNULL(cl.CuratedLink, 0)                        AS CuratedLink,
        irf.AffindaEmbedUrl,
        irf.AffindaEmbedUrlCreatedAt
    FROM       dbo.IRF irf
    LEFT JOIN  dbo.Client cl ON cl.id = irf.ClientId
    LEFT JOIN  dbo.UserList ulh ON ulh.id = irf.RisebirdHr
    LEFT JOIN  ConfirmedMeetings cm ON cm.IrfIntId = irf.id
    LEFT JOIN  TotalMeetings tm ON tm.IrfIntId = irf.id
    LEFT JOIN  SelectedMeetings sm ON sm.IrfIntId = irf.id
    LEFT JOIN  ScheduledMeetings sch ON sch.IrfIntId = irf.id
    LEFT JOIN  ViewerEmails ve ON ve.IrfIntId = irf.id
    WHERE (@IrfIntId IS NULL OR irf.id = @IrfIntId)
      AND (@IrfIntId IS NOT NULL OR ISNULL(irf.IsDeleted, 0) = 0);
END
GO
