CREATE OR ALTER PROCEDURE [dbo].[usp_GetTransInterviewerContextForInterview]

    @InterviewId INT,

    @IrfId       INT = 0

AS

BEGIN

    SET NOCOUNT ON;



    ;WITH InterviewRow AS (

        SELECT im.id, LTRIM(RTRIM(im.interviewer)) AS InterviewerRaw, im.IRFId

        FROM dbo.InterviewMeetings im

        WHERE im.id = @InterviewId

    ),

    ResolvedUser AS (

        SELECT TOP 1 u.UserId, u.EmailId, u.Priority

        FROM InterviewRow ir

        CROSS APPLY (

            SELECT ul.id AS UserId, ul.EmailId, 1 AS Priority

            FROM dbo.UserList ul

            WHERE LTRIM(RTRIM(ul.EmailId)) = ir.InterviewerRaw

            UNION ALL

            SELECT ul.id, ul.EmailId, 2

            FROM dbo.UserList ul

            WHERE ul.EmailId = ir.InterviewerRaw

            UNION ALL

            SELECT ip.UserId, ul.EmailId, 3

            FROM dbo.InterviewerProfile ip

            INNER JOIN dbo.UserList ul ON ul.id = ip.UserId

            WHERE LTRIM(RTRIM(ip.NickName)) = ir.InterviewerRaw

        ) u

        WHERE ir.InterviewerRaw IS NOT NULL AND ir.InterviewerRaw <> ''

        ORDER BY u.Priority

    )

    SELECT TOP 1

        ti.id              AS TransInterviewerId,

        ru.UserId          AS UserId,

        ti.IRFId           AS IrfId,

        ti.IsActive        AS IsActive,

        ti.ClientAction    AS ClientAction,

        ti.AutoQcVerdict   AS AutoQcVerdict,

        ru.EmailId         AS InterviewerEmail,

        ip.NickName        AS InterviewerCodedId,

        CASE WHEN ti.IsSMEVerified = 1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsRbVetYes,

        CASE WHEN ti.IsSMEVerified = 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS RbVetAlreadyNo

    FROM InterviewRow ir

    INNER JOIN ResolvedUser ru ON 1 = 1

    INNER JOIN dbo.IRF irf

        ON (@IrfId > 0 AND irf.id = @IrfId)

        OR (@IrfId <= 0 AND irf.IRFId = ir.IRFId)

    INNER JOIN dbo.TransInterviewer ti

        ON ti.IRFId = irf.id

       AND (

            ti.UserId = ru.UserId

            OR EXISTS (SELECT 1 FROM dbo.Interviewer iv WHERE iv.id = ti.InterviewerId AND iv.UserId = ru.UserId)

            OR EXISTS (SELECT 1 FROM dbo.InterviewerProfile ipr WHERE ipr.id = ti.InterviewerId AND ipr.UserId = ru.UserId)

       )

    LEFT JOIN dbo.InterviewerProfile ip ON ip.UserId = ru.UserId

    ORDER BY ti.id;

END

GO

