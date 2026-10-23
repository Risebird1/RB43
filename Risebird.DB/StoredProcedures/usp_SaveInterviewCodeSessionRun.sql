IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveInterviewCodeSessionRun'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewCodeSessionRun
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Persists the latest run of a code session: the source that was run, what the runner returned
-- (already capped by CodeExecutionService) and the backend status. The runner itself stays read-only;
-- this is the session's own record of what came back.
CREATE PROCEDURE [dbo].[usp_SaveInterviewCodeSessionRun]
    @Token       UNIQUEIDENTIFIER,
    @LanguageId  VARCHAR(40),
    @Source      NVARCHAR(MAX),
    @Output      NVARCHAR(MAX),
    @Status      VARCHAR(40),
    @CurrentDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.InterviewCodeSessions
       SET LanguageId = @LanguageId,
           LastRunSource = @Source,
           LastRunOutput = @Output,
           LastRunStatus = @Status,
           RunCount = RunCount + 1,
           OpenedOn = ISNULL(OpenedOn, @CurrentDate)
     WHERE Token = @Token;

    SELECT RunCount FROM dbo.InterviewCodeSessions WHERE Token = @Token;
END
GO
