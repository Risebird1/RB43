IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_RegisterProctoringSession'))
BEGIN
    DROP PROCEDURE dbo.usp_RegisterProctoringSession
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Called when a launch token is minted. Makes the session token "recognized" so that
-- POST /api/proctoring/session/join no longer 401s for it.
CREATE PROCEDURE [dbo].[usp_RegisterProctoringSession]
    @SessionToken VARCHAR(64),
    @InterviewId NVARCHAR(100),
    @CandidateEmail NVARCHAR(256),
    @BrowserSessionId NVARCHAR(200),
    @Trust VARCHAR(16),
    @ExpiresAtUtc DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ProctoringSessions
        (SessionToken, InterviewId, CandidateEmail, BrowserSessionId, Trust, State, ExpiresAtUtc)
    VALUES
        (@SessionToken, @InterviewId, @CandidateEmail, @BrowserSessionId, @Trust, 'idle', @ExpiresAtUtc)
END
GO
