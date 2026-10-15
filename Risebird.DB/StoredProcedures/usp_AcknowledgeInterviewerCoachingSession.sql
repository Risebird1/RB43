/*
================================================================================
usp_AcknowledgeInterviewerCoachingSession — "I've read this" on the interviewer's
My Quality page.

Sets InterviewerCoachingSession.AcknowledgedAtUtc only when the session belongs to
the caller (@Email is the session email, never a request value) and has not been
acknowledged yet. Idempotent: a second call changes nothing.

Returns one row: Result = 1 acknowledged now, 0 already acknowledged,
-1 not found / not the caller's session, -2 column not deployed.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_AcknowledgeInterviewerCoachingSession]
    @SessionId INT,
    @Email     VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SET @Email = NULLIF(LTRIM(RTRIM(@Email)), '');

    IF OBJECT_ID(N'dbo.InterviewerCoachingSession', N'U') IS NULL
       OR COL_LENGTH('dbo.InterviewerCoachingSession', 'AcknowledgedAtUtc') IS NULL
    BEGIN
        SELECT -2 AS Result;
        RETURN;
    END

    DECLARE @Result INT;

    EXEC sp_executesql N'
        IF NOT EXISTS (
            SELECT 1 FROM dbo.InterviewerCoachingSession s
            WHERE s.Id = @SessionId
              AND s.InterviewerUserId IN (SELECT id FROM dbo.UserList WHERE EmailId = @Email))
            SET @Result = -1;
        ELSE
        BEGIN
            UPDATE dbo.InterviewerCoachingSession
            SET AcknowledgedAtUtc = GETUTCDATE()
            WHERE Id = @SessionId AND AcknowledgedAtUtc IS NULL;
            SET @Result = CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END;
        END',
        N'@SessionId INT, @Email VARCHAR(150), @Result INT OUTPUT',
        @SessionId = @SessionId, @Email = @Email, @Result = @Result OUTPUT;

    SELECT @Result AS Result;
END
GO
