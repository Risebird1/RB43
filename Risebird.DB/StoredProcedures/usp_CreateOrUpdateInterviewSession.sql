IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_CreateOrUpdateInterviewSession'))
BEGIN
    DROP PROCEDURE dbo.usp_CreateOrUpdateInterviewSession
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_CreateOrUpdateInterviewSession
    @SessionId      UNIQUEIDENTIFIER,
    @ThreadId       NVARCHAR(255),
    @AssistantId    NVARCHAR(255),
    @CandidateEmail NVARCHAR(255),
    @CreatedAt      DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM InterviewSessions WHERE SessionId = @SessionId)
    BEGIN
        -- Session already exists (e.g. page refresh after thread creation failed mid-way).
        -- Update the ThreadId so the new thread is picked up correctly.
        UPDATE InterviewSessions
        SET ThreadId    = @ThreadId,
            AssistantId = @AssistantId
        WHERE SessionId = @SessionId;
    END
    ELSE
    BEGIN
        INSERT INTO InterviewSessions (SessionId, ThreadId, AssistantId, CandidateEmail, CreatedAt)
        VALUES (@SessionId, @ThreadId, @AssistantId, @CandidateEmail, @CreatedAt);
    END
END
GO
