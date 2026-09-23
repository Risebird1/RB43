IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewCodeSession'))
BEGIN
    DROP PROCEDURE [dbo].[usp_SaveInterviewCodeSession]
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================================================
-- Saves candidate work, and optionally submits.
--
-- Two callers, one procedure:
--   @Submit = 0  a debounced autosave while the candidate types. Durability matters more
--                than tidiness here: the code has to survive a closed tab, a refresh or a
--                dropped connection, so every save writes the full buffer.
--   @Submit = 1  the submit. The editor goes read-only and the session freezes.
--
-- A frozen session is never rewritten. WHERE SessionState <> 4 AND not past ExpiresAt is
-- what makes each attempt freeze independently: an earlier session left open must not be
-- reopened by a late autosave, and must not block a later one.
--
-- Stored output must match submitted code. The caller re-runs once on submit and passes
-- the fresh result, or passes RunState = 3 (NotExecuted). Never attach an earlier run's
-- output to code that has since changed.
-- =============================================================================
CREATE PROCEDURE [dbo].[usp_SaveInterviewCodeSession]
    @SessionToken        varchar(64),
    @SubmittedCode       nvarchar(max) = null,
    @Language            varchar(50)   = null,
    @LanguageVersionUsed varchar(50)   = null,
    @Stdout              nvarchar(max) = null,
    @Stderr              nvarchar(max) = null,
    @ExitStatus          int           = null,
    @RunState            int           = null,
    @RunDurationMs       int           = null,
    @PasteEvents         int           = null,
    @Submit              bit           = 0,
    @TimeNow             datetime      = null
AS
BEGIN
    SET NOCOUNT ON;
    IF @TimeNow IS NULL SET @TimeNow = GETDATE();

    UPDATE InterviewCodeSession
       SET SubmittedCode       = ISNULL(@SubmittedCode, SubmittedCode),
           Language            = ISNULL(@Language, Language),
           -- Follows the run, exactly as the output columns below do. Keeping the previous
           -- run's version against a fresh result would state that this attempt ran under a
           -- runtime it never touched - and an attempt that did not run at all would keep
           -- advertising a version as though it had.
           LanguageVersionUsed = CASE WHEN @RunState IS NULL THEN LanguageVersionUsed ELSE @LanguageVersionUsed END,
           Stdout              = CASE WHEN @RunState IS NULL THEN Stdout      ELSE @Stdout END,
           Stderr              = CASE WHEN @RunState IS NULL THEN Stderr      ELSE @Stderr END,
           ExitStatus          = CASE WHEN @RunState IS NULL THEN ExitStatus  ELSE @ExitStatus END,
           RunState            = ISNULL(@RunState, RunState),
           RunDurationMs       = CASE WHEN @RunState IS NULL THEN RunDurationMs ELSE @RunDurationMs END,
           -- Paste count only ever grows; a reload must not reset it.
           PasteEvents         = CASE WHEN @PasteEvents IS NULL OR @PasteEvents < PasteEvents
                                      THEN PasteEvents ELSE @PasteEvents END,
           LastSavedAt         = @TimeNow,
           SubmittedAt         = CASE WHEN @Submit = 1 THEN @TimeNow ELSE SubmittedAt END,
           SessionState        = CASE WHEN @Submit = 1 THEN 4
                                      WHEN SessionState < 3 THEN 3
                                      ELSE SessionState END
     WHERE SessionToken = @SessionToken
       AND SessionState <> 4
       AND (ExpiresAt IS NULL OR ExpiresAt > @TimeNow);

    SELECT CAST(CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END AS bit) AS Saved;
END
GO
