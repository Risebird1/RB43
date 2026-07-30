IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCallCatalogueIndexBatch'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCallCatalogueIndexBatch
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns denormalized call catalogue rows for Azure AI Search (callcatalogueindex).
-- Performance notes:
--   - IRF join uses UNION ALL (numeric id vs string IRFId) instead of OR (avoids scan).
--   - ViewerEmails is built only for IRFs present in the candidate catalogue batch.
--   - No ORDER BY on full crawl (indexing does not require sort).
CREATE PROCEDURE [dbo].[usp_GetCallCatalogueIndexBatch]
    @DocumentKey NVARCHAR(300) = NULL,
    @IrfIntId INT = NULL,
    @CandidateEmail NVARCHAR(150) = NULL,
    @InterviewerEmail NVARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH RawCandidateCalls AS (
        -- Direct calls: CallRecordingDetails.IRFId stores numeric IRF.id as varchar
        SELECT
            IRF.id AS IrfIntId,
            IRF.IRFId,
            crd.CandidateEmail,
            (ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, '')) AS CandidateName,
            IRF.JDTiltle,
            CASE
                WHEN CHARINDEX('|', IRF.JDTiltle) > 0
                THEN LTRIM(RTRIM(SUBSTRING(IRF.JDTiltle, 1, CHARINDEX('|', IRF.JDTiltle) - 1)))
                ELSE LTRIM(RTRIM(IRF.JDTiltle))
            END AS PrimarySkill,
            cl.Name AS ClientName,
            crd.CalledOn,
            crd.id AS CallerId
        FROM dbo.CallRecordingDetails crd
        INNER JOIN dbo.IRF ON TRY_CAST(crd.IRFId AS INT) = IRF.id
        INNER JOIN dbo.Client cl ON cl.id = IRF.ClientId
        LEFT JOIN dbo.UserList ul ON ul.EmailId = crd.CandidateEmail
        WHERE crd.interviewid = 0
          AND TRY_CAST(crd.IRFId AS INT) IS NOT NULL
          AND (@IrfIntId IS NULL OR IRF.id = @IrfIntId)
          AND (@CandidateEmail IS NULL OR crd.CandidateEmail = @CandidateEmail)

        UNION ALL

        -- Direct calls: CallRecordingDetails.IRFId stores string IRF.IRFId (legacy rows)
        SELECT
            IRF.id,
            IRF.IRFId,
            crd.CandidateEmail,
            (ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, '')),
            IRF.JDTiltle,
            CASE
                WHEN CHARINDEX('|', IRF.JDTiltle) > 0
                THEN LTRIM(RTRIM(SUBSTRING(IRF.JDTiltle, 1, CHARINDEX('|', IRF.JDTiltle) - 1)))
                ELSE LTRIM(RTRIM(IRF.JDTiltle))
            END,
            cl.Name,
            crd.CalledOn,
            crd.id
        FROM dbo.CallRecordingDetails crd
        INNER JOIN dbo.IRF ON crd.IRFId = IRF.IRFId
        INNER JOIN dbo.Client cl ON cl.id = IRF.ClientId
        LEFT JOIN dbo.UserList ul ON ul.EmailId = crd.CandidateEmail
        WHERE crd.interviewid = 0
          AND TRY_CAST(crd.IRFId AS INT) IS NULL
          AND (@IrfIntId IS NULL OR IRF.id = @IrfIntId)
          AND (@CandidateEmail IS NULL OR crd.CandidateEmail = @CandidateEmail)

        UNION ALL

        -- Calls linked via InterviewMeetings
        SELECT
            IRF.id,
            IRF.IRFId,
            im.candidateEmail,
            (ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, '')),
            IRF.JDTiltle,
            CASE
                WHEN CHARINDEX('|', IRF.JDTiltle) > 0
                THEN LTRIM(RTRIM(SUBSTRING(IRF.JDTiltle, 1, CHARINDEX('|', IRF.JDTiltle) - 1)))
                ELSE LTRIM(RTRIM(IRF.JDTiltle))
            END,
            cl.Name,
            crd.CalledOn,
            crd.id
        FROM dbo.CallRecordingDetails crd
        INNER JOIN dbo.InterviewMeetings im ON im.id = crd.Interviewid
        INNER JOIN dbo.IRF ON im.IRFId = IRF.IRFId
        INNER JOIN dbo.Client cl ON cl.id = IRF.ClientId
        LEFT JOIN dbo.UserList ul ON ul.EmailId = im.candidateEmail
        WHERE crd.interviewid <> 0
          AND (@IrfIntId IS NULL OR IRF.id = @IrfIntId)
          AND (@CandidateEmail IS NULL OR im.candidateEmail = @CandidateEmail)
    ),
    CandidateCatalogue AS (
        SELECT
            cc.IrfIntId,
            cc.IRFId,
            cc.CandidateEmail,
            cc.CandidateName,
            cc.JDTiltle,
            cc.PrimarySkill,
            cc.ClientName,
            cc.CalledOn,
            crd.WhoCalled,
            CONCAT(
                'c-',
                cc.IrfIntId,
                '-',
                REPLACE(REPLACE(REPLACE(LOWER(LTRIM(RTRIM(cc.CandidateEmail))), '@', '_at_'), '.', '_'), ' ', '_')
            ) AS DocumentKey
        FROM (
            SELECT
                IrfIntId,
                IRFId,
                CandidateEmail,
                CandidateName,
                JDTiltle,
                PrimarySkill,
                ClientName,
                MAX(CalledOn) AS CalledOn,
                MAX(CallerId) AS CallerId
            FROM RawCandidateCalls
            GROUP BY IrfIntId, IRFId, CandidateEmail, CandidateName, JDTiltle, PrimarySkill, ClientName
        ) cc
        INNER JOIN dbo.CallRecordingDetails crd ON crd.id = cc.CallerId
    ),
    ViewerEmails AS (
        SELECT
            v.IrfIntId,
            STRING_AGG(v.EmailId, '|') WITHIN GROUP (ORDER BY v.EmailId) AS AllowedViewerEmails
        FROM (
            SELECT DISTINCT th.IRFId AS IrfIntId, LOWER(LTRIM(RTRIM(ul.EmailId))) AS EmailId
            FROM dbo.TransHR th
            INNER JOIN dbo.HR hr ON hr.id = th.HRId
            INNER JOIN dbo.UserList ul ON ul.id = hr.UserId
            INNER JOIN CandidateCatalogue cc ON cc.IrfIntId = th.IRFId
            WHERE ul.EmailId IS NOT NULL

            UNION

            SELECT DISTINCT cc.IrfIntId, LOWER(LTRIM(RTRIM(ulh.EmailId)))
            FROM CandidateCatalogue cc
            INNER JOIN dbo.IRF irf ON irf.id = cc.IrfIntId
            INNER JOIN dbo.UserList ulh ON ulh.id = irf.RisebirdHr
            WHERE ulh.EmailId IS NOT NULL

            UNION

            SELECT DISTINCT cc.IrfIntId, LOWER(LTRIM(RTRIM(ulsh.EmailId)))
            FROM CandidateCatalogue cc
            INNER JOIN dbo.IRF irf ON irf.id = cc.IrfIntId
            INNER JOIN dbo.SeniorHR sh ON sh.ClientId = irf.ClientId
            INNER JOIN dbo.UserList ulsh ON ulsh.id = sh.UserId
            WHERE ulsh.EmailId IS NOT NULL
        ) v
        GROUP BY v.IrfIntId
    ),
    InterviewerCatalogue AS (
        SELECT
            ic.CandidateEmail,
            ic.CandidateName,
            ic.CalledOn,
            crd.WhoCalled,
            CONCAT(
                'i-',
                REPLACE(REPLACE(REPLACE(LOWER(LTRIM(RTRIM(ic.CandidateEmail))), '@', '_at_'), '.', '_'), ' ', '_')
            ) AS DocumentKey
        FROM (
            SELECT
                crd.Interviewer AS CandidateEmail,
                (ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, '')) AS CandidateName,
                MAX(crd.CalledOn) AS CalledOn,
                MAX(crd.id) AS CallerId
            FROM dbo.CallRecordingInterviewer crd
            LEFT JOIN dbo.UserList ul ON ul.EmailId = crd.Interviewer
            WHERE @IrfIntId IS NULL
              AND (@InterviewerEmail IS NULL OR crd.Interviewer = @InterviewerEmail)
            GROUP BY crd.Interviewer, (ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))
        ) ic
        INNER JOIN dbo.CallRecordingInterviewer crd ON crd.id = ic.CallerId
    )
    SELECT
        c.DocumentKey,
        'Candidate' AS CatalogueType,
        c.IrfIntId,
        c.IRFId,
        c.CandidateEmail,
        c.CandidateName,
        c.JDTiltle,
        c.PrimarySkill,
        c.ClientName,
        c.CalledOn,
        c.WhoCalled,
        CAST(1 AS BIT) AS AllowAllInternal,
        ve.AllowedViewerEmails
    FROM CandidateCatalogue c
    LEFT JOIN ViewerEmails ve ON ve.IrfIntId = c.IrfIntId
    WHERE @InterviewerEmail IS NULL
      AND (
            (@DocumentKey IS NULL
             AND (@IrfIntId IS NULL OR c.IrfIntId = @IrfIntId)
             AND (@CandidateEmail IS NULL OR c.CandidateEmail = @CandidateEmail))
         OR (@DocumentKey IS NOT NULL AND c.DocumentKey = @DocumentKey)
          )

    UNION ALL

    SELECT
        i.DocumentKey,
        'Interviewer' AS CatalogueType,
        0 AS IrfIntId,
        CAST(NULL AS VARCHAR(50)) AS IRFId,
        i.CandidateEmail,
        i.CandidateName,
        CAST(NULL AS VARCHAR(500)) AS JDTiltle,
        CAST(NULL AS VARCHAR(500)) AS PrimarySkill,
        CAST(NULL AS VARCHAR(200)) AS ClientName,
        i.CalledOn,
        i.WhoCalled,
        CAST(1 AS BIT) AS AllowAllInternal,
        CAST(NULL AS VARCHAR(MAX)) AS AllowedViewerEmails
    FROM InterviewerCatalogue i
    WHERE @IrfIntId IS NULL
      AND @CandidateEmail IS NULL
      AND (
            (@DocumentKey IS NULL
             AND (@InterviewerEmail IS NULL OR i.CandidateEmail = @InterviewerEmail))
         OR (@DocumentKey IS NOT NULL AND i.DocumentKey = @DocumentKey)
          );
END
GO
