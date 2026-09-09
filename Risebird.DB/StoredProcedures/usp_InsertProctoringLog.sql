IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertProctoringLog'))
BEGIN
    DROP PROCEDURE dbo.usp_InsertProctoringLog
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Idempotent on (ProctoringSessionId, Sequence): the desktop client's local durable queue can
-- redeliver the same batch after a crash/restart, so a duplicate is a no-op that reports
-- "not inserted" rather than an error, letting the caller skip re-scoring it. Resolves the
-- caller's SessionToken to ProctoringSessions.id first, same as usp_SaveAICodeInterviewReport
-- resolves SessionId to a ReportId before touching the child table.
CREATE PROCEDURE [dbo].[usp_InsertProctoringLog]
    @SessionToken VARCHAR(64),
    @Sequence INT,
    @Type VARCHAR(64),
    @Category NVARCHAR(200),
    @Level VARCHAR(16),
    @Confidence VARCHAR(16),
    @OpenedAtUtc DATETIME2(3),
    @ResolvedAtUtc DATETIME2(3) = NULL,
    @RedactedDetail NVARCHAR(200) = NULL,
    @RawDetail NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProctoringSessionId INT;
    SELECT @ProctoringSessionId = [id] FROM dbo.ProctoringSessions WHERE [SessionToken] = @SessionToken;

    IF @ProctoringSessionId IS NULL
    BEGIN
        SELECT CAST(0 AS BIT) AS Inserted;
        RETURN;
    END

    IF NOT EXISTS (
        SELECT 1 FROM dbo.ProctoringLogs
        WHERE [ProctoringSessionId] = @ProctoringSessionId AND [Sequence] = @Sequence
    )
    BEGIN
        INSERT INTO dbo.ProctoringLogs
            (ProctoringSessionId, [Sequence], [Type], Category, [Level], Confidence, OpenedAtUtc, ResolvedAtUtc, RedactedDetail, RawDetail)
        VALUES
            (@ProctoringSessionId, @Sequence, @Type, @Category, @Level, @Confidence, @OpenedAtUtc, @ResolvedAtUtc, @RedactedDetail, @RawDetail);

        SELECT CAST(1 AS BIT) AS Inserted
    END
    ELSE
    BEGIN
        SELECT CAST(0 AS BIT) AS Inserted
    END
END
GO
