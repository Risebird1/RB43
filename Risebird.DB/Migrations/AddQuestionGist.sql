/* =====================================================================
   Question gist for coding questions.

   Synthesia narrates a question's text to the candidate. Source code read
   aloud verbatim is unintelligible, so a coding question instead carries a
   plain-language gist and that is what gets sent to Synthesia.

   Run this whole script once, top to bottom, against the application
   database. It is idempotent and safe to re-run. The final SELECT reports
   the state of every object it touches.
   ===================================================================== */

-- ---------------------------------------------------------------------
-- 1. QuestionGist column
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.columns
               WHERE Name = N'QuestionGist'
                 AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions ADD QuestionGist nvarchar(max) NULL;
END
GO

-- ---------------------------------------------------------------------
-- 2. Table type V6 — adds QuestionGist to the bulk-insert type.
--    A new version is created rather than altering V5, because a table type
--    cannot be dropped while a procedure still references it.
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'AddIRFSampleQuestions_TableTypeV6')
BEGIN
    CREATE TYPE [dbo].[AddIRFSampleQuestions_TableTypeV6] AS TABLE(
        [IRFId] [int] NOT NULL,
        [Skill] [int] NULL,
        [Comment] [nvarchar](max) NULL,
        [CreatedBy] [varchar](150) NULL,
        [SkillName] [nvarchar](500) NULL,
        [ExpectedAnswer] [nvarchar](max) NULL,
        [IsCodingQuestion] [bit] NULL,
        [CodingTimeLimitInMinutes] [int] NULL,
        [QuestionGist] [nvarchar](max) NULL
    );
END
GO

-- ---------------------------------------------------------------------
-- 3. Bulk add — switch to the V6 type and persist the gist
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[usp_AddIRFSampleQuestions]
(
    @IRFSampleQuestionsBulkData AS [dbo].[AddIRFSampleQuestions_TableTypeV6] READONLY,
    @IRFId int
)
AS
BEGIN
    BEGIN TRY
        BEGIN TRAN

        INSERT INTO SkillChannelSampleQuestions
            ([IRFId], [Skill], [Comment], CreatedBy, SkillName, ExpectedAnswer, IsCodingQuestion, CodingTimeLimitInMinutes, QuestionGist)
        SELECT tscbd.IRFId, tscbd.Skill, tscbd.Comment, tscbd.CreatedBy, tscbd.SkillName, tscbd.ExpectedAnswer,
               ISNULL(tscbd.IsCodingQuestion, 0),
               CASE WHEN ISNULL(tscbd.IsCodingQuestion, 0) = 1 THEN tscbd.CodingTimeLimitInMinutes ELSE NULL END,
               tscbd.QuestionGist
        FROM @IRFSampleQuestionsBulkData tscbd;

        COMMIT TRAN
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),
                @Severity int = ERROR_SEVERITY(),
                @State smallint = ERROR_STATE()
        RAISERROR (@Message, @Severity, @State)
    END CATCH
END
GO

-- ---------------------------------------------------------------------
-- 4. Edit an existing question
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateSkillChannelSampleQuestion]
    @id int,
    @comment nvarchar(max),
    @expectedAnswer nvarchar(max) = NULL,
    @isCodingQuestion bit = 0,
    @codingTimeLimitInMinutes int = NULL,
    @questionGist nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE SkillChannelSampleQuestions
    SET Comment = @comment,
        ExpectedAnswer = @expectedAnswer,
        IsCodingQuestion = ISNULL(@isCodingQuestion, 0),
        CodingTimeLimitInMinutes = CASE WHEN ISNULL(@isCodingQuestion, 0) = 1 THEN @codingTimeLimitInMinutes ELSE NULL END,
        -- Keep any previously written gist when none is supplied, so an edit that does not
        -- touch the gist does not silently discard it.
        QuestionGist = ISNULL(@questionGist, QuestionGist)
    WHERE id = @id;
END
GO

-- ---------------------------------------------------------------------
-- 5. Inline coding toggle in the questions panel
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[usp_SetSampleQuestionCodingMode]
    @id int,
    @isCodingQuestion bit = 0,
    @codingTimeLimitInMinutes int = NULL,
    @questionGist nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE SkillChannelSampleQuestions
    SET IsCodingQuestion = ISNULL(@isCodingQuestion, 0),
        CodingTimeLimitInMinutes = CASE WHEN ISNULL(@isCodingQuestion, 0) = 1 THEN @codingTimeLimitInMinutes ELSE NULL END,
        -- The gist survives un-ticking coding mode: it is prose the user typed, and
        -- re-ticking should not force them to write it again.
        QuestionGist = ISNULL(@questionGist, QuestionGist)
    WHERE id = @id;
END
GO

/* ---------------------------------------------------------------------
   Verification — every row should report OK.
   --------------------------------------------------------------------- */
SELECT 'SkillChannelSampleQuestions.QuestionGist' AS [Object],
       CASE WHEN EXISTS (SELECT * FROM sys.columns
                         WHERE Name = N'QuestionGist'
                           AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
            THEN 'OK' ELSE 'MISSING' END AS Status
UNION ALL SELECT 'AddIRFSampleQuestions_TableTypeV6',
       CASE WHEN EXISTS (SELECT * FROM sys.types
                         WHERE is_table_type = 1 AND name = 'AddIRFSampleQuestions_TableTypeV6')
            THEN 'OK' ELSE 'MISSING' END
UNION ALL SELECT 'usp_AddIRFSampleQuestions writes QuestionGist',
       CASE WHEN OBJECT_DEFINITION(OBJECT_ID(N'usp_AddIRFSampleQuestions')) LIKE '%QuestionGist%'
            THEN 'OK' ELSE 'STALE' END
UNION ALL SELECT 'usp_UpdateSkillChannelSampleQuestion accepts @questionGist',
       CASE WHEN OBJECT_DEFINITION(OBJECT_ID(N'usp_UpdateSkillChannelSampleQuestion')) LIKE '%questionGist%'
            THEN 'OK' ELSE 'STALE' END
UNION ALL SELECT 'usp_SetSampleQuestionCodingMode accepts @questionGist',
       CASE WHEN OBJECT_DEFINITION(OBJECT_ID(N'usp_SetSampleQuestionCodingMode')) LIKE '%questionGist%'
            THEN 'OK' ELSE 'STALE' END
UNION ALL SELECT 'Coding questions still missing a gist',
       CAST((SELECT COUNT(*) FROM SkillChannelSampleQuestions
             WHERE ISNULL(IsCodingQuestion, 0) = 1
               AND (QuestionGist IS NULL OR LTRIM(RTRIM(QuestionGist)) = '')) AS varchar(20))
       + ' question(s) — add a gist before generating their videos';
