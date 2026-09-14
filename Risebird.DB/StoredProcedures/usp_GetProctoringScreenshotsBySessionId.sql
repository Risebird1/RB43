IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetProctoringScreenshotsBySessionId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetProctoringScreenshotsBySessionId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Screenshots for one session, for the dashboard's full-page Proctoring Session Report.
CREATE PROCEDURE [dbo].[usp_GetProctoringScreenshotsBySessionId]
    @ProctoringSessionId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [id], [ProctoringSessionId], [Sequence], [BlobUrl], [ContentType], [CapturedAtUtc], [CreatedAtUtc]
    FROM dbo.Screenshots
    WHERE [ProctoringSessionId] = @ProctoringSessionId
    ORDER BY [Sequence] ASC
END
GO
