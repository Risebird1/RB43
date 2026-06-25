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
        ul.EmailId         AS InterviewerEmail
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    INNER JOIN dbo.TransInterviewer ti ON ti.IRFId = irf.id
    INNER JOIN dbo.UserList ul ON ul.id = ti.UserId AND ul.EmailId = im.interviewer
    WHERE im.id = @InterviewId
    ORDER BY ti.id;
END
GO
