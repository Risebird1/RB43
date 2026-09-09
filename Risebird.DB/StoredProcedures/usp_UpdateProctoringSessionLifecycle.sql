IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateProctoringSessionLifecycle'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateProctoringSessionLifecycle
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Merges usp_MarkProctoringSessionJoined/Started/Ended into one procedure, keyed by @Event.
-- @Event = 'joined' -> sets JoinedAtUtc (+ CandidateEmail), State = 'starting'
-- @Event = 'started' -> sets StartedAtUtc,                  State = 'running'
-- @Event = 'ended'   -> sets EndedAtUtc,                    State = 'stopped'
CREATE PROCEDURE [dbo].[usp_UpdateProctoringSessionLifecycle]
    @SessionToken VARCHAR(64),
    @Event VARCHAR(16),
    @CandidateEmail NVARCHAR(256) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Event = 'joined'
    BEGIN
        UPDATE dbo.ProctoringSessions
        SET [JoinedAtUtc] = ISNULL([JoinedAtUtc], SYSUTCDATETIME()),
            [CandidateEmail] = ISNULL(@CandidateEmail, [CandidateEmail])
        WHERE [SessionToken] = @SessionToken

        EXEC dbo.usp_UpdateProctoringSessionState @SessionToken = @SessionToken, @State = 'starting'
    END
    ELSE IF @Event = 'started'
    BEGIN
        UPDATE dbo.ProctoringSessions
        SET [StartedAtUtc] = ISNULL([StartedAtUtc], SYSUTCDATETIME())
        WHERE [SessionToken] = @SessionToken

        EXEC dbo.usp_UpdateProctoringSessionState @SessionToken = @SessionToken, @State = 'running'
    END
    ELSE IF @Event = 'ended'
    BEGIN
        UPDATE dbo.ProctoringSessions
        SET [EndedAtUtc] = ISNULL([EndedAtUtc], SYSUTCDATETIME())
        WHERE [SessionToken] = @SessionToken

        EXEC dbo.usp_UpdateProctoringSessionState @SessionToken = @SessionToken, @State = 'stopped'
    END
END
GO
