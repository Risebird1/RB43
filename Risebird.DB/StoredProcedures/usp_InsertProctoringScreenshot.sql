IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertProctoringScreenshot'))
BEGIN
    DROP PROCEDURE dbo.usp_InsertProctoringScreenshot
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Idempotent on (ProctoringSessionId, Sequence) — same pattern as usp_InsertProctoringLog.
-- Resolves the caller's SessionToken to ProctoringSessions.id first.
CREATE PROCEDURE [dbo].[usp_InsertProctoringScreenshot]
    @SessionToken VARCHAR(64),
    @Sequence INT,
    @BlobUrl NVARCHAR(1000),
    @ContentType VARCHAR(100) = NULL,
    @CapturedAtUtc DATETIME2(3)
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
        SELECT 1 FROM dbo.Screenshots
        WHERE [ProctoringSessionId] = @ProctoringSessionId AND [Sequence] = @Sequence
    )
    BEGIN
        INSERT INTO dbo.Screenshots
            (ProctoringSessionId, [Sequence], BlobUrl, ContentType, CapturedAtUtc)
        VALUES
            (@ProctoringSessionId, @Sequence, @BlobUrl, @ContentType, @CapturedAtUtc);

        SELECT CAST(1 AS BIT) AS Inserted
    END
    ELSE
    BEGIN
        SELECT CAST(0 AS BIT) AS Inserted
    END
END
GO
