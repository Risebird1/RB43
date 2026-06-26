CREATE OR ALTER PROCEDURE [dbo].[usp_GetTransInterviewerContextForInterview]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        ti.id              AS TransInterviewerId,
        ti.UserId          AS UserId,
        ti.IRFId           AS IrfId,
        ti.IsActive        AS IsActive,
        ti.ClientAction    AS ClientAction,
        ul.EmailId         AS InterviewerEmail,
        ip.NickName        AS InterviewerCodedId,
        CASE
            WHEN ti.IsSMEVerified = 1 THEN CAST(1 AS BIT)
            ELSE CAST(0 AS BIT)
        END                AS IsRbVetYes,
        CASE
            WHEN ti.IsSMEVerified = 0 THEN CAST(1 AS BIT)
            ELSE CAST(0 AS BIT)
        END                AS RbVetAlreadyNo
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    INNER JOIN dbo.UserList ul ON ul.EmailId = im.interviewer
    INNER JOIN dbo.TransInterviewer ti ON ti.IRFId = irf.id AND ti.UserId = ul.id
    INNER JOIN dbo.InterviewerProfile ip ON ip.UserId = ti.UserId
    WHERE im.id = @InterviewId
    ORDER BY ti.id;
END
GO
