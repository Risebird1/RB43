/* =====================================================================
   Coding question support for AI Interview (Preset) — full deployment.

   Run this ONE script to enable coding questions end to end. It is
   idempotent and safe to re-run, and it applies the objects in
   dependency order (columns -> table type -> procedures).

   The symptom of a partial deployment is subtle: the admin panel reads
   SkillChannelSampleQuestions with SELECT *, so the "Coding" badge shows
   as soon as the columns exist, while usp_GetRandomQuestionForSession
   uses an explicit column list and therefore keeps returning
   IsCodingQuestion = 0 until it is recreated. The interview then serves
   the question as an ordinary spoken question with no code editor.
   ===================================================================== */

-- ---------------------------------------------------------------------
-- 1. Question flags on SkillChannelSampleQuestions
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.columns
               WHERE Name = N'IsCodingQuestion'
                 AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions ADD IsCodingQuestion bit NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns
               WHERE Name = N'CodingTimeLimitInMinutes'
                 AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions ADD CodingTimeLimitInMinutes int NULL;
END
GO

-- Adding a nullable column with a default does not backfill existing rows, so rows
-- created before this migration hold NULL. Code that reads the table with SELECT *
-- maps the flag onto a non-nullable bool, which fails on NULL, so settle them at 0.
UPDATE SkillChannelSampleQuestions
SET IsCodingQuestion = 0
WHERE IsCodingQuestion IS NULL;
GO

-- ---------------------------------------------------------------------
-- 2. CodingAssessment result table
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'CodingAssessment')
BEGIN
    CREATE TABLE [dbo].[CodingAssessment](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [SessionId] [varchar](50) NULL,
        [QuestionId] [int] NULL,
        [QuestionText] [nvarchar](max) NULL,
        [CodeLanguage] [varchar](50) NULL,
        [SubmittedCode] [nvarchar](max) NULL,
        [TimeLimitInMinutes] [int] NULL,
        [TimeStarted] [datetime] NULL,
        [TimeCompleted] [datetime] NULL,
        [TimeTakenInSeconds] [decimal](10,2) NULL,
        [IsTimeExpired] [bit] NULL,
        [StdInput] [nvarchar](max) NULL,
        [LastRunOutput] [nvarchar](max) NULL,
        [LastRunStatus] [varchar](100) NULL,
        [RunCount] [int] NULL,
        [CreatedAt] [datetime] NULL,
        [CreatedBy] [varchar](150) NULL,
     CONSTRAINT [PK_CodingAssessment] PRIMARY KEY CLUSTERED ([id] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY];
END
GO

-- Columns added after the table first shipped
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'StdInput' AND Object_ID = Object_ID(N'CodingAssessment'))
    ALTER TABLE CodingAssessment ADD StdInput nvarchar(max) NULL;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'LastRunOutput' AND Object_ID = Object_ID(N'CodingAssessment'))
    ALTER TABLE CodingAssessment ADD LastRunOutput nvarchar(max) NULL;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'LastRunStatus' AND Object_ID = Object_ID(N'CodingAssessment'))
    ALTER TABLE CodingAssessment ADD LastRunStatus varchar(100) NULL;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'RunCount' AND Object_ID = Object_ID(N'CodingAssessment'))
    ALTER TABLE CodingAssessment ADD RunCount int NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes
               WHERE name = 'UQ_CodingAssessment_SessionQuestion'
                 AND object_id = OBJECT_ID('CodingAssessment'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_CodingAssessment_SessionQuestion
        ON CodingAssessment (SessionId, QuestionId);
END
GO

-- ---------------------------------------------------------------------
-- 3. Bulk insert table type (must exist before usp_AddIRFSampleQuestions)
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'AddIRFSampleQuestions_TableTypeV5')
BEGIN
    CREATE TYPE [dbo].[AddIRFSampleQuestions_TableTypeV5] AS TABLE(
        [IRFId] [int] NOT NULL,
        [Skill] [int] NULL,
        [Comment] [nvarchar](max) NULL,
        [CreatedBy] [varchar](150) NULL,
        [SkillName] [nvarchar](500) NULL,
        [ExpectedAnswer] [nvarchar](max) NULL,
        [IsCodingQuestion] [bit] NULL,
        [CodingTimeLimitInMinutes] [int] NULL
    );
END
GO

-- ---------------------------------------------------------------------
-- 4. Serve the coding flag to the interview runtime.
--    This is the step most likely to be missing.
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE usp_GetRandomQuestionForSession
    @interviewId int,
    @irfId int
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SessionId UNIQUEIDENTIFIER =
        (SELECT SessionId FROM InterviewMeetings WHERE id = @interviewId);

    DECLARE @AskedCount INT = (
        SELECT COUNT(*)
        FROM TranscriptsConv
        WHERE SessionId = @SessionId AND [Role] = 'assistant'
    );

    IF (@AskedCount >= 35)
    BEGIN
        SELECT NULL AS [Message], NULL AS AIQuestionVideoLink, NULL AS Id;
        RETURN;
    END;

    ;WITH OrderedQuestions AS (
        SELECT q.id, q.Comment, q.AIQuestionVideoLink, q.IsTypingSpeedAssessment,
               q.IsListeningComprehension, q.ListeningAudioUrl, q.ListeningAudioTranscription,
               q.IsCodingQuestion, q.CodingTimeLimitInMinutes,
               ROW_NUMBER() OVER (ORDER BY q.Id) AS RowNum
        FROM SkillChannelSampleQuestions q
        WHERE q.IRFId = @irfId
    )
    SELECT Comment AS [Message], AIQuestionVideoLink, id,
           ISNULL(IsTypingSpeedAssessment, 0) AS IsTypingSpeedAssessment,
           ISNULL(IsListeningComprehension, 0) AS IsListeningComprehension,
           ListeningAudioUrl,
           ListeningAudioTranscription,
           ISNULL(IsCodingQuestion, 0) AS IsCodingQuestion,
           CodingTimeLimitInMinutes
    FROM OrderedQuestions
    WHERE RowNum = @AskedCount + 1;
END
GO

-- ---------------------------------------------------------------------
-- 5. Persist the coding flag when questions are added in bulk
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[usp_AddIRFSampleQuestions]
(
    @IRFSampleQuestionsBulkData AS [dbo].[AddIRFSampleQuestions_TableTypeV5] READONLY,
    @IRFId int
)
AS
BEGIN
    BEGIN TRY
        BEGIN TRAN

        INSERT INTO SkillChannelSampleQuestions
            ([IRFId], [Skill], [Comment], CreatedBy, SkillName, ExpectedAnswer, IsCodingQuestion, CodingTimeLimitInMinutes)
        SELECT tscbd.IRFId, tscbd.Skill, tscbd.Comment, tscbd.CreatedBy, tscbd.SkillName, tscbd.ExpectedAnswer,
               ISNULL(tscbd.IsCodingQuestion, 0),
               CASE WHEN ISNULL(tscbd.IsCodingQuestion, 0) = 1 THEN tscbd.CodingTimeLimitInMinutes ELSE NULL END
        FROM @IRFSampleQuestionsBulkData tscbd;

        COMMIT TRAN
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),
                @Severity int = ERROR_SEVERITY(),
                @State smallint = ERROR_STATE();
        RAISERROR (@Message, @Severity, @State);
    END CATCH
END
GO

-- ---------------------------------------------------------------------
-- 6. Persist the coding flag when an existing question is edited
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateSkillChannelSampleQuestion]
    @id int,
    @comment nvarchar(max),
    @expectedAnswer nvarchar(max) = NULL,
    @isCodingQuestion bit = 0,
    @codingTimeLimitInMinutes int = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE SkillChannelSampleQuestions
    SET Comment = @comment,
        ExpectedAnswer = @expectedAnswer,
        IsCodingQuestion = ISNULL(@isCodingQuestion, 0),
        CodingTimeLimitInMinutes = CASE WHEN ISNULL(@isCodingQuestion, 0) = 1 THEN @codingTimeLimitInMinutes ELSE NULL END
    WHERE id = @id;
END
GO

-- ---------------------------------------------------------------------
-- 7. Toggle coding mode on an existing question without touching its text
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[usp_SetSampleQuestionCodingMode]
    @id int,
    @isCodingQuestion bit = 0,
    @codingTimeLimitInMinutes int = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE SkillChannelSampleQuestions
    SET IsCodingQuestion = ISNULL(@isCodingQuestion, 0),
        CodingTimeLimitInMinutes = CASE WHEN ISNULL(@isCodingQuestion, 0) = 1 THEN @codingTimeLimitInMinutes ELSE NULL END
    WHERE id = @id;
END
GO

-- ---------------------------------------------------------------------
-- 8. Save the candidate's coding submission
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE usp_SaveCodingAssessment
    @SessionId VARCHAR(50),
    @QuestionId INT,
    @QuestionText NVARCHAR(MAX) = NULL,
    @CodeLanguage VARCHAR(50) = NULL,
    @SubmittedCode NVARCHAR(MAX) = NULL,
    @TimeLimitInMinutes INT = NULL,
    @TimeStarted DATETIME = NULL,
    @TimeCompleted DATETIME = NULL,
    @TimeTakenInSeconds DECIMAL(10,2) = NULL,
    @IsTimeExpired BIT = 0,
    @StdInput NVARCHAR(MAX) = NULL,
    @LastRunOutput NVARCHAR(MAX) = NULL,
    @LastRunStatus VARCHAR(100) = NULL,
    @RunCount INT = 0,
    @CreatedBy VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ExistingId INT;
    SELECT @ExistingId = id
    FROM CodingAssessment
    WHERE SessionId = @SessionId AND QuestionId = @QuestionId;

    IF @ExistingId IS NOT NULL
    BEGIN
        UPDATE CodingAssessment
        SET QuestionText = @QuestionText,
            CodeLanguage = @CodeLanguage,
            SubmittedCode = @SubmittedCode,
            TimeLimitInMinutes = @TimeLimitInMinutes,
            TimeStarted = @TimeStarted,
            TimeCompleted = @TimeCompleted,
            TimeTakenInSeconds = @TimeTakenInSeconds,
            IsTimeExpired = @IsTimeExpired,
            StdInput = @StdInput,
            LastRunOutput = @LastRunOutput,
            LastRunStatus = @LastRunStatus,
            RunCount = @RunCount,
            CreatedBy = @CreatedBy
        WHERE id = @ExistingId;

        SELECT @ExistingId AS NewId;
    END
    ELSE
    BEGIN
        INSERT INTO CodingAssessment (
            SessionId, QuestionId, QuestionText, CodeLanguage, SubmittedCode,
            TimeLimitInMinutes, TimeStarted, TimeCompleted, TimeTakenInSeconds,
            IsTimeExpired, StdInput, LastRunOutput, LastRunStatus, RunCount,
            CreatedAt, CreatedBy
        )
        VALUES (
            @SessionId, @QuestionId, @QuestionText, @CodeLanguage, @SubmittedCode,
            @TimeLimitInMinutes, @TimeStarted, @TimeCompleted, @TimeTakenInSeconds,
            @IsTimeExpired, @StdInput, @LastRunOutput, @LastRunStatus, @RunCount,
            GETDATE(), @CreatedBy
        );

        SELECT SCOPE_IDENTITY() AS NewId;
    END
END
GO

/* ---------------------------------------------------------------------
   Verification — every row should report OK.
   --------------------------------------------------------------------- */
SELECT 'SkillChannelSampleQuestions.IsCodingQuestion' AS [Object],
       CASE WHEN EXISTS (SELECT * FROM sys.columns
                         WHERE Name = N'IsCodingQuestion'
                           AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
            THEN 'OK' ELSE 'MISSING' END AS Status
UNION ALL SELECT 'SkillChannelSampleQuestions.CodingTimeLimitInMinutes',
       CASE WHEN EXISTS (SELECT * FROM sys.columns
                         WHERE Name = N'CodingTimeLimitInMinutes'
                           AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
            THEN 'OK' ELSE 'MISSING' END
UNION ALL SELECT 'CodingAssessment table',
       CASE WHEN OBJECT_ID(N'CodingAssessment') IS NOT NULL THEN 'OK' ELSE 'MISSING' END
UNION ALL SELECT 'AddIRFSampleQuestions_TableTypeV5',
       CASE WHEN EXISTS (SELECT * FROM sys.types
                         WHERE is_table_type = 1 AND name = 'AddIRFSampleQuestions_TableTypeV5')
            THEN 'OK' ELSE 'MISSING' END
UNION ALL SELECT 'usp_GetRandomQuestionForSession returns IsCodingQuestion',
       CASE WHEN OBJECT_DEFINITION(OBJECT_ID(N'usp_GetRandomQuestionForSession')) LIKE '%IsCodingQuestion%'
            THEN 'OK' ELSE 'STALE - coding questions will not open the IDE' END
UNION ALL SELECT 'usp_AddIRFSampleQuestions writes IsCodingQuestion',
       CASE WHEN OBJECT_DEFINITION(OBJECT_ID(N'usp_AddIRFSampleQuestions')) LIKE '%IsCodingQuestion%'
            THEN 'OK' ELSE 'STALE' END
UNION ALL SELECT 'usp_SetSampleQuestionCodingMode',
       CASE WHEN OBJECT_ID(N'usp_SetSampleQuestionCodingMode') IS NOT NULL THEN 'OK' ELSE 'MISSING' END
UNION ALL SELECT 'usp_SaveCodingAssessment accepts run columns',
       CASE WHEN OBJECT_DEFINITION(OBJECT_ID(N'usp_SaveCodingAssessment')) LIKE '%LastRunOutput%'
            THEN 'OK' ELSE 'STALE' END;
GO
