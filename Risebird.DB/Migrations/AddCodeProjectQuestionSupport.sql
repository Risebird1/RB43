/* =====================================================================
   Code project question support for AI Interview — full deployment.

   A third question kind alongside spoken and coding questions. The
   candidate gets a provisioned VM with a small runnable project instead
   of the in-page editor. IsCodeProjectQuestion sits ALONGSIDE
   IsCodingQuestion, never replacing it: one opens an editor, the other
   opens a machine.

   Run this ONE script. It is idempotent and safe to re-run, and applies
   objects in dependency order (columns -> tables -> constraint -> procs).

   Same trap AddCodingQuestionSupport.sql documents: the admin panel reads
   SkillChannelSampleQuestions with SELECT * onto a non-nullable bool, so
   the new flag is backfilled, and usp_GetRandomQuestionForSession uses an
   explicit column list, so it is recreated here.
   ===================================================================== */

-- ---------------------------------------------------------------------
-- 1. Question flag on SkillChannelSampleQuestions
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.columns
               WHERE Name = N'IsCodeProjectQuestion'
                 AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions ADD IsCodeProjectQuestion bit NULL DEFAULT 0;
END
GO

UPDATE SkillChannelSampleQuestions
SET IsCodeProjectQuestion = 0
WHERE IsCodeProjectQuestion IS NULL;
GO

-- ---------------------------------------------------------------------
-- 2. StackImages — allowlist of pre-built VM images.
--    Only IsActive = 1 rows enter the generator's prompt. Starts empty:
--    the generator cannot run until at least one image is baked and added.
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'StackImages')
BEGIN
    CREATE TABLE [dbo].[StackImages](
        [id]             [int] IDENTITY(1,1) NOT NULL,
        [Code]           [varchar](50)  NOT NULL,
        [DisplayName]    [varchar](200) NOT NULL,
        [Toolchain]      [varchar](500) NOT NULL,
        [GalleryImageId] [varchar](500) NOT NULL,
        [OsType]         [varchar](25)  NOT NULL,
        [IsActive]       [bit]          NOT NULL CONSTRAINT DF_StackImages_IsActive DEFAULT 1,
     CONSTRAINT [PK_StackImages] PRIMARY KEY CLUSTERED ([id] ASC),
     CONSTRAINT [UQ_StackImages_Code] UNIQUE ([Code])
    ) ON [PRIMARY];
END
GO

-- ---------------------------------------------------------------------
-- 3. CodeProjectDetails — the project delta, 1:1 with the question row
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'CodeProjectDetails')
BEGIN
    CREATE TABLE [dbo].[CodeProjectDetails](
        [QuestionId]        [int]           NOT NULL,
        [Language]          [varchar](50)   NOT NULL,
        [Framework]         [varchar](50)   NULL,
        [StackImageCode]    [varchar](50)   NOT NULL,
        [ProjectBlobUri]    [varchar](500)  NULL,   -- candidate-visible zip
        [SecretsBlobUri]    [varchar](500)  NULL,   -- hidden tests + reference solution, never on the VM
        [EntryPointFile]    [varchar](500)  NULL,
        [ReadOnlyPathsJson] [nvarchar](max) NULL,
        [BuildCommand]      [varchar](500)  NULL,
        [RunCommand]        [varchar](500)  NULL,
        [TestCommand]       [varchar](500)  NULL,
        [HasWebserver]      [bit]           NULL,
        [PortsJson]         [varchar](200)  NULL,
        [RequirementsJson]  [nvarchar](max) NULL,
        [EstimatedMinutes]  [int]           NULL,
        [ValidationStatus]  [varchar](20)   NOT NULL CONSTRAINT DF_CodeProjectDetails_ValidationStatus DEFAULT 'pending',
        [ValidationJson]    [nvarchar](max) NULL,
        [ValidatedAtUtc]    [datetime2]     NULL,
        [ApprovedBySmeId]   [int]           NULL,
        [ApprovedAtUtc]     [datetime2]     NULL,
     CONSTRAINT [PK_CodeProjectDetails] PRIMARY KEY CLUSTERED ([QuestionId] ASC),
     CONSTRAINT [FK_CodeProjectDetails_Question] FOREIGN KEY ([QuestionId])
        REFERENCES [dbo].[SkillChannelSampleQuestions]([id]) ON DELETE CASCADE,
     CONSTRAINT [FK_CodeProjectDetails_StackImage] FOREIGN KEY ([StackImageCode])
        REFERENCES [dbo].[StackImages]([Code])
    ) ON [PRIMARY];
END
GO

-- A question not proved to build must never reach a candidate. Enforced in the
-- database so no application path can publish without a project zip.
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = N'CK_CodeProjectDetails_Servable')
BEGIN
    ALTER TABLE CodeProjectDetails ADD CONSTRAINT CK_CodeProjectDetails_Servable
        CHECK (ValidationStatus IN ('pending','validating','failed')
            OR (ValidationStatus = 'passed' AND ProjectBlobUri IS NOT NULL));
END
GO

-- ---------------------------------------------------------------------
-- 4. CodingAssessment — project results
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'VMPropertyId' AND Object_ID = Object_ID(N'CodingAssessment'))
    ALTER TABLE CodingAssessment ADD VMPropertyId int NULL;   -- FK -> VMProperties
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'RequirementsPassed' AND Object_ID = Object_ID(N'CodingAssessment'))
    ALTER TABLE CodingAssessment ADD RequirementsPassed int NULL;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'RequirementsTotal' AND Object_ID = Object_ID(N'CodingAssessment'))
    ALTER TABLE CodingAssessment ADD RequirementsTotal int NULL;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'ResultsJson' AND Object_ID = Object_ID(N'CodingAssessment'))
    ALTER TABLE CodingAssessment ADD ResultsJson nvarchar(max) NULL;
GO

-- ---------------------------------------------------------------------
-- 5. Serve the project flag to the interview runtime, and never serve a
--    project question whose ValidationStatus <> 'passed'.
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
               q.IsCodingQuestion, q.CodingTimeLimitInMinutes, q.IsCodeProjectQuestion,
               ROW_NUMBER() OVER (ORDER BY q.Id) AS RowNum
        FROM SkillChannelSampleQuestions q
        LEFT JOIN CodeProjectDetails cpd ON cpd.QuestionId = q.id
        WHERE q.IRFId = @irfId
          AND (ISNULL(q.IsCodeProjectQuestion, 0) = 0 OR cpd.ValidationStatus = 'passed')
    )
    SELECT Comment AS [Message], AIQuestionVideoLink, id,
           ISNULL(IsTypingSpeedAssessment, 0) AS IsTypingSpeedAssessment,
           ISNULL(IsListeningComprehension, 0) AS IsListeningComprehension,
           ListeningAudioUrl,
           ListeningAudioTranscription,
           ISNULL(IsCodingQuestion, 0) AS IsCodingQuestion,
           CodingTimeLimitInMinutes,
           ISNULL(IsCodeProjectQuestion, 0) AS IsCodeProjectQuestion
    FROM OrderedQuestions
    WHERE RowNum = @AskedCount + 1;
END
GO

-- Set-bound counterpart: same output columns, same filter.
CREATE OR ALTER PROCEDURE [dbo].[usp_GetNextAIQuestionForInterview]
    @interviewId int
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SessionId UNIQUEIDENTIFIER = (SELECT SessionId FROM InterviewMeetings WHERE id = @interviewId);
    DECLARE @SetId int = (SELECT SetId FROM InterviewAIQuestionSet WHERE InterviewId = @interviewId);

    IF @SetId IS NULL OR @SessionId IS NULL
        RETURN;

    DECLARE @AskedCount INT = (
        SELECT COUNT(*)
        FROM TranscriptsConv
        WHERE SessionId = @SessionId AND [Role] = 'assistant'
    );

    ;WITH SetQuestions AS (
        SELECT q.id, q.Comment, q.AIQuestionVideoLink, q.IsTypingSpeedAssessment,
               q.IsListeningComprehension, q.ListeningAudioUrl, q.ListeningAudioTranscription,
               q.IsCodingQuestion, q.CodingTimeLimitInMinutes, q.IsCodeProjectQuestion,
               ROW_NUMBER() OVER (ORDER BY i.id) AS RowNum
        FROM AIQuestionSetItems i
        JOIN SkillChannelSampleQuestions q ON q.id = i.QuestionId
        LEFT JOIN CodeProjectDetails cpd ON cpd.QuestionId = q.id
        WHERE i.SetId = @SetId
          AND (ISNULL(q.IsCodeProjectQuestion, 0) = 0 OR cpd.ValidationStatus = 'passed')
    )
    SELECT Comment AS [Message], AIQuestionVideoLink, id,
           ISNULL(IsTypingSpeedAssessment, 0) AS IsTypingSpeedAssessment,
           ISNULL(IsListeningComprehension, 0) AS IsListeningComprehension,
           ListeningAudioUrl,
           ListeningAudioTranscription,
           ISNULL(IsCodingQuestion, 0) AS IsCodingQuestion,
           CodingTimeLimitInMinutes,
           ISNULL(IsCodeProjectQuestion, 0) AS IsCodeProjectQuestion
    FROM SetQuestions
    WHERE RowNum = @AskedCount + 1;
END
GO

-- Look-ahead: while the candidate is on question N-1, report whether question N
-- is a code project question so its VM can start before the candidate arrives.
-- Uses exactly the same ordering and filter as usp_GetRandomQuestionForSession.
CREATE OR ALTER PROCEDURE usp_PeekNextCodeProjectQuestionForSession
    @interviewId int,
    @irfId int,
    @currentQuestionId int
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SetId int = (SELECT SetId FROM InterviewAIQuestionSet WHERE InterviewId = @interviewId);

    -- Mirror whichever picker serves this interview (the bound set, else the channel pool),
    -- with the same ordering and filter, then take the question right after the current one.
    ;WITH OrderedQuestions AS (
        SELECT q.id, ISNULL(q.IsCodeProjectQuestion, 0) AS IsCodeProjectQuestion, cpd.StackImageCode,
               ROW_NUMBER() OVER (ORDER BY i.id) AS RowNum
        FROM AIQuestionSetItems i
        JOIN SkillChannelSampleQuestions q ON q.id = i.QuestionId
        LEFT JOIN CodeProjectDetails cpd ON cpd.QuestionId = q.id
        WHERE @SetId IS NOT NULL AND i.SetId = @SetId
          AND (ISNULL(q.IsCodeProjectQuestion, 0) = 0 OR cpd.ValidationStatus = 'passed')
        UNION ALL
        SELECT q.id, ISNULL(q.IsCodeProjectQuestion, 0), cpd.StackImageCode,
               ROW_NUMBER() OVER (ORDER BY q.Id)
        FROM SkillChannelSampleQuestions q
        LEFT JOIN CodeProjectDetails cpd ON cpd.QuestionId = q.id
        WHERE @SetId IS NULL AND q.IRFId = @irfId
          AND (ISNULL(q.IsCodeProjectQuestion, 0) = 0 OR cpd.ValidationStatus = 'passed')
    )
    SELECT nxt.id AS QuestionId, nxt.StackImageCode
    FROM OrderedQuestions cur
    JOIN OrderedQuestions nxt ON nxt.RowNum = cur.RowNum + 1
    WHERE cur.id = @currentQuestionId AND nxt.IsCodeProjectQuestion = 1;
END
GO

-- ---------------------------------------------------------------------
-- 6. Generator procs
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE usp_GetActiveStackImages
AS
BEGIN
    SET NOCOUNT ON;
    SELECT id, Code, DisplayName, Toolchain, GalleryImageId, OsType, IsActive
    FROM StackImages
    WHERE IsActive = 1
    ORDER BY Code;
END
GO

-- Question row + project delta in one transaction, status 'validating'.
-- GeneratedPayload must hold the sanitized tier 1/2 payload only: the admin panel
-- reads this table with SELECT *, and hidden tests / reference solution must not leak.
CREATE OR ALTER PROCEDURE usp_AddCodeProjectQuestion
    @IRFId int,
    @SkillName nvarchar(500),
    @Comment nvarchar(max),
    @ExpectedAnswer nvarchar(max) = NULL,
    @QuestionGist nvarchar(max) = NULL,
    @TimeLimitInMinutes int,
    @GeneratedPayload nvarchar(max) = NULL,
    @CreatedBy varchar(150),
    @Language varchar(50),
    @Framework varchar(50) = NULL,
    @StackImageCode varchar(50),
    @EntryPointFile varchar(500) = NULL,
    @ReadOnlyPathsJson nvarchar(max) = NULL,
    @BuildCommand varchar(500) = NULL,
    @RunCommand varchar(500) = NULL,
    @TestCommand varchar(500) = NULL,
    @HasWebserver bit = NULL,
    @PortsJson varchar(200) = NULL,
    @RequirementsJson nvarchar(max) = NULL,
    @EstimatedMinutes int = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN

        INSERT INTO SkillChannelSampleQuestions
            (IRFId, Skill, SkillName, Comment, ExpectedAnswer, GeneratedPayload, IsGeneratedByAi, CreatedBy,
             QuestionGist, IsCodingQuestion, IsCodeProjectQuestion, CodingTimeLimitInMinutes)
        VALUES
            (@IRFId, 0, @SkillName, @Comment, @ExpectedAnswer, @GeneratedPayload, 1, @CreatedBy,
             @QuestionGist, 0, 1, @TimeLimitInMinutes);

        DECLARE @NewId int = SCOPE_IDENTITY();

        INSERT INTO CodeProjectDetails
            (QuestionId, Language, Framework, StackImageCode, EntryPointFile, ReadOnlyPathsJson,
             BuildCommand, RunCommand, TestCommand, HasWebserver, PortsJson, RequirementsJson,
             EstimatedMinutes, ValidationStatus)
        VALUES
            (@NewId, @Language, @Framework, @StackImageCode, @EntryPointFile, @ReadOnlyPathsJson,
             @BuildCommand, @RunCommand, @TestCommand, @HasWebserver, @PortsJson, @RequirementsJson,
             @EstimatedMinutes, 'validating');

        COMMIT TRAN
        SELECT @NewId AS NewId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),
                @Severity int = ERROR_SEVERITY(),
                @State smallint = ERROR_STATE();
        RAISERROR (@Message, @Severity, @State);
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE usp_UpdateCodeProjectValidation
    @QuestionId int,
    @ValidationStatus varchar(20),
    @ValidationJson nvarchar(max) = NULL,
    @ProjectBlobUri varchar(500) = NULL,
    @SecretsBlobUri varchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CodeProjectDetails
    SET ValidationStatus = @ValidationStatus,
        ValidationJson   = @ValidationJson,
        ProjectBlobUri   = ISNULL(@ProjectBlobUri, ProjectBlobUri),
        SecretsBlobUri   = ISNULL(@SecretsBlobUri, SecretsBlobUri),
        ValidatedAtUtc   = CASE WHEN @ValidationStatus IN ('passed','failed') THEN SYSUTCDATETIME() ELSE ValidatedAtUtc END
    WHERE QuestionId = @QuestionId;
    SELECT @@ROWCOUNT AS Affected;
END
GO

CREATE OR ALTER PROCEDURE usp_GetCodeProjectDetails
    @QuestionId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT cpd.*, q.IRFId, q.SkillName, q.Comment, q.QuestionGist, q.CodingTimeLimitInMinutes, q.GeneratedPayload
    FROM CodeProjectDetails cpd
    INNER JOIN SkillChannelSampleQuestions q ON q.id = cpd.QuestionId
    WHERE cpd.QuestionId = @QuestionId;
END
GO

-- Projects left in 'pending'/'validating' (e.g. the app restarted mid-job) so the worker can resume them.
CREATE OR ALTER PROCEDURE usp_GetUnfinishedCodeProjectValidations
AS
BEGIN
    SET NOCOUNT ON;
    SELECT cpd.QuestionId
    FROM CodeProjectDetails cpd
    WHERE cpd.ValidationStatus IN ('pending','validating');
END
GO

CREATE OR ALTER PROCEDURE usp_GetCodeProjectStatusesByIRF
    @IRFId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT cpd.QuestionId, cpd.ValidationStatus, cpd.StackImageCode, cpd.EstimatedMinutes
    FROM CodeProjectDetails cpd
    INNER JOIN SkillChannelSampleQuestions q ON q.id = cpd.QuestionId
    WHERE q.IRFId = @IRFId;
END
GO

-- ---------------------------------------------------------------------
-- 7. Candidate result for a code project question
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE usp_SaveCodeProjectAssessment
    @SessionId VARCHAR(50),
    @QuestionId INT,
    @QuestionText NVARCHAR(MAX) = NULL,
    @CodeLanguage VARCHAR(50) = NULL,
    @SubmittedCode NVARCHAR(MAX) = NULL,     -- the candidate's diff against the baseline commit
    @TimeLimitInMinutes INT = NULL,
    @TimeStarted DATETIME = NULL,
    @TimeCompleted DATETIME = NULL,
    @TimeTakenInSeconds DECIMAL(10,2) = NULL,
    @IsTimeExpired BIT = 0,
    @VMPropertyId INT = NULL,
    @RequirementsPassed INT = NULL,
    @RequirementsTotal INT = NULL,
    @ResultsJson NVARCHAR(MAX) = NULL,
    @CreatedBy VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ExistingId INT;
    SELECT @ExistingId = id FROM CodingAssessment
    WHERE SessionId = @SessionId AND QuestionId = @QuestionId;

    IF @ExistingId IS NOT NULL
    BEGIN
        UPDATE CodingAssessment
        SET QuestionText = ISNULL(@QuestionText, QuestionText),
            CodeLanguage = ISNULL(@CodeLanguage, CodeLanguage),
            SubmittedCode = ISNULL(@SubmittedCode, SubmittedCode),
            TimeLimitInMinutes = ISNULL(@TimeLimitInMinutes, TimeLimitInMinutes),
            TimeStarted = ISNULL(@TimeStarted, TimeStarted),
            TimeCompleted = ISNULL(@TimeCompleted, TimeCompleted),
            TimeTakenInSeconds = ISNULL(@TimeTakenInSeconds, TimeTakenInSeconds),
            IsTimeExpired = ISNULL(@IsTimeExpired, IsTimeExpired),
            VMPropertyId = ISNULL(@VMPropertyId, VMPropertyId),
            RequirementsPassed = ISNULL(@RequirementsPassed, RequirementsPassed),
            RequirementsTotal = ISNULL(@RequirementsTotal, RequirementsTotal),
            ResultsJson = ISNULL(@ResultsJson, ResultsJson)
        WHERE id = @ExistingId;
        SELECT @ExistingId AS NewId;
    END
    ELSE
    BEGIN
        INSERT INTO CodingAssessment (
            SessionId, QuestionId, QuestionText, CodeLanguage, SubmittedCode,
            TimeLimitInMinutes, TimeStarted, TimeCompleted, TimeTakenInSeconds, IsTimeExpired,
            VMPropertyId, RequirementsPassed, RequirementsTotal, ResultsJson, CreatedAt, CreatedBy)
        VALUES (
            @SessionId, @QuestionId, @QuestionText, @CodeLanguage, @SubmittedCode,
            @TimeLimitInMinutes, @TimeStarted, @TimeCompleted, @TimeTakenInSeconds, @IsTimeExpired,
            @VMPropertyId, @RequirementsPassed, @RequirementsTotal, @ResultsJson, GETDATE(), @CreatedBy);
        SELECT CAST(SCOPE_IDENTITY() AS int) AS NewId;
    END
END
GO

CREATE OR ALTER PROCEDURE usp_GetCodeProjectAssessment
    @SessionId VARCHAR(50),
    @QuestionId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT id, SessionId, QuestionId, QuestionText, CodeLanguage, SubmittedCode, TimeLimitInMinutes,
           TimeStarted, TimeCompleted, TimeTakenInSeconds, IsTimeExpired, VMPropertyId,
           RequirementsPassed, RequirementsTotal, ResultsJson
    FROM CodingAssessment
    WHERE SessionId = @SessionId AND QuestionId = @QuestionId;
END
GO

-- ---------------------------------------------------------------------
-- 8. CodeProjectEnvironments — one candidate machine per (session, question).
--    Look-ahead creates the row while the candidate is on question N-1; the
--    clock starts at HandedOverAtUtc, not at provisioning.
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'CodeProjectEnvironments')
BEGIN
    CREATE TABLE [dbo].[CodeProjectEnvironments](
        [id]                [int] IDENTITY(1,1) NOT NULL,
        [SessionId]         [varchar](50)   NOT NULL,
        [QuestionId]        [int]           NOT NULL,
        [StackImageCode]    [varchar](50)   NULL,
        [State]             [varchar](20)   NOT NULL,   -- requested | provisioning | ready | handedover | submitted | failed | released
        [EnvironmentHandle] [varchar](200)  NULL,       -- provisioner's id for the machine
        [ConnectUrl]        [varchar](1000) NULL,
        [VMPropertyId]      [int]           NULL,
        [Message]           [nvarchar](500) NULL,
        [RequestedAtUtc]    [datetime2]     NOT NULL CONSTRAINT DF_CodeProjectEnvironments_RequestedAtUtc DEFAULT SYSUTCDATETIME(),
        [ReadyAtUtc]        [datetime2]     NULL,
        [HandedOverAtUtc]   [datetime2]     NULL,
        [SubmittedAtUtc]    [datetime2]     NULL,
     CONSTRAINT [PK_CodeProjectEnvironments] PRIMARY KEY CLUSTERED ([id] ASC),
     CONSTRAINT [UQ_CodeProjectEnvironments_SessionQuestion] UNIQUE ([SessionId], [QuestionId])
    ) ON [PRIMARY];
END
GO

-- Claims the (session, question) slot. Returns Claimed = 1 only to the first caller, so the
-- look-ahead and the on-arrival path never provision two machines for the same question.
CREATE OR ALTER PROCEDURE usp_ClaimCodeProjectEnvironment
    @SessionId varchar(50),
    @QuestionId int,
    @StackImageCode varchar(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Claimed bit = 0;
    BEGIN TRY
        INSERT INTO CodeProjectEnvironments (SessionId, QuestionId, StackImageCode, State)
        SELECT @SessionId, @QuestionId, @StackImageCode, 'requested'
        WHERE NOT EXISTS (SELECT 1 FROM CodeProjectEnvironments WITH (UPDLOCK, HOLDLOCK)
                          WHERE SessionId = @SessionId AND QuestionId = @QuestionId);
        IF @@ROWCOUNT = 1 SET @Claimed = 1;
    END TRY
    BEGIN CATCH
        -- Unique key race: someone else claimed it first.
        IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
    END CATCH
    SELECT @Claimed AS Claimed;
END
GO

CREATE OR ALTER PROCEDURE usp_UpdateCodeProjectEnvironment
    @SessionId varchar(50),
    @QuestionId int,
    @State varchar(20),
    @EnvironmentHandle varchar(200) = NULL,
    @ConnectUrl varchar(1000) = NULL,
    @VMPropertyId int = NULL,
    @Message nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CodeProjectEnvironments
    SET State = @State,
        EnvironmentHandle = ISNULL(@EnvironmentHandle, EnvironmentHandle),
        ConnectUrl = ISNULL(@ConnectUrl, ConnectUrl),
        VMPropertyId = ISNULL(@VMPropertyId, VMPropertyId),
        Message = @Message,
        ReadyAtUtc = CASE WHEN @State = 'ready' AND ReadyAtUtc IS NULL THEN SYSUTCDATETIME() ELSE ReadyAtUtc END,
        -- Set once: a reconnect must not restart the candidate's clock.
        HandedOverAtUtc = CASE WHEN @State = 'handedover' AND HandedOverAtUtc IS NULL THEN SYSUTCDATETIME() ELSE HandedOverAtUtc END,
        SubmittedAtUtc = CASE WHEN @State = 'submitted' AND SubmittedAtUtc IS NULL THEN SYSUTCDATETIME() ELSE SubmittedAtUtc END
    WHERE SessionId = @SessionId AND QuestionId = @QuestionId;
    SELECT @@ROWCOUNT AS Affected;
END
GO

CREATE OR ALTER PROCEDURE usp_GetCodeProjectEnvironment
    @SessionId varchar(50),
    @QuestionId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT id, SessionId, QuestionId, StackImageCode, State, EnvironmentHandle, ConnectUrl, VMPropertyId,
           Message, RequestedAtUtc, ReadyAtUtc, HandedOverAtUtc, SubmittedAtUtc
    FROM CodeProjectEnvironments
    WHERE SessionId = @SessionId AND QuestionId = @QuestionId;
END
GO

CREATE OR ALTER PROCEDURE usp_GetCodeProjectAssessmentsBySessionId
    @SessionId varchar(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ca.id, ca.SessionId, ca.QuestionId, ca.QuestionText, ca.CodeLanguage, ca.SubmittedCode,
           ca.TimeLimitInMinutes, ca.TimeStarted, ca.TimeCompleted, ca.TimeTakenInSeconds, ca.IsTimeExpired,
           ca.VMPropertyId, ca.RequirementsPassed, ca.RequirementsTotal, ca.ResultsJson
    FROM CodingAssessment ca
    INNER JOIN CodeProjectDetails cpd ON cpd.QuestionId = ca.QuestionId
    WHERE ca.SessionId = @SessionId
    ORDER BY ca.id;
END
GO

-- ---------------------------------------------------------------------
-- 9. Set creation pool (StoredProcedures\usp_GetAIQuestionPoolForSets.sql)
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[usp_GetAIQuestionPoolForSets]
	@IRFId int
AS
BEGIN
    SET NOCOUNT ON;

    -- A preset AI interview channel is IsAIInterview = 1 with AIInterviewType = 2 (Preset).
    -- Reported as its own result set so the caller can say precisely why creation is unavailable
    -- rather than inferring it from an empty pool.
    DECLARE @IsPreset bit =
        CASE WHEN EXISTS (SELECT 1 FROM IRF
                          WHERE id = @IRFId
                            AND ISNULL(IsAIInterview, 0) = 1
                            AND ISNULL(AIInterviewType, 0) = 2)
             THEN 1 ELSE 0 END;

    SELECT @IsPreset AS IsPreset;

    -- Anything that is not preset has no papers to deal, whoever the caller is.
    IF @IsPreset = 0
    BEGIN
        SELECT CAST(NULL AS nvarchar(500)) AS SkillName, CAST(NULL AS int) AS ParameterOrder WHERE 1 = 0;
        SELECT CAST(NULL AS int) AS id, CAST(NULL AS nvarchar(500)) AS SkillName,
               CAST(NULL AS nvarchar(max)) AS Title, CAST(NULL AS int) AS MaxTimeMinutes,
               CAST(NULL AS int) AS MaxScore, CAST(NULL AS nvarchar(100)) AS Difficulty,
               CAST(0 AS bit) AS IsCodingQuestion WHERE 1 = 0;
        RETURN;
    END

    -- Rubric parameters are the distinct TransTechSkill.SkillName values for the channel.
    -- ParameterOrder fixes the ask order of questions inside every set.
    SELECT LTRIM(RTRIM(tts.SkillName)) AS SkillName,
           ROW_NUMBER() OVER (ORDER BY MIN(tts.id)) AS ParameterOrder
    FROM TransTechSkill tts
    WHERE tts.IRFId = @IRFId
      AND tts.SkillName IS NOT NULL
      AND LTRIM(RTRIM(tts.SkillName)) <> ''
    GROUP BY LTRIM(RTRIM(tts.SkillName))
    ORDER BY MIN(tts.id);

    DECLARE @MaxTimeMinutes nvarchar(200) = 'CAST(NULL AS int)';
    DECLARE @MaxScore nvarchar(200) = 'CAST(NULL AS int)';
    DECLARE @Difficulty nvarchar(200) = 'CAST(NULL AS nvarchar(100))';

    IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'MaxTimeMinutes') IS NOT NULL
        SET @MaxTimeMinutes = 'q.MaxTimeMinutes';
    ELSE IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'CodingTimeLimitInMinutes') IS NOT NULL
        SET @MaxTimeMinutes = 'q.CodingTimeLimitInMinutes';

    IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'MaxScore') IS NOT NULL
        SET @MaxScore = 'q.MaxScore';

    IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'Difficulty') IS NOT NULL
        SET @Difficulty = 'CAST(q.Difficulty AS nvarchar(100))';

    DECLARE @IsCoding nvarchar(200) = 'CAST(0 AS bit)';

    IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'IsCodingQuestion') IS NOT NULL
        SET @IsCoding = 'CAST(ISNULL(q.IsCodingQuestion, 0) AS bit)';

    -- Code project questions: never deal one into a set before it has passed validation, and
    -- count a validated one as hands-on so set creation spreads them like coding questions.
    DECLARE @ProjectFilter nvarchar(400) = N'';
    IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'IsCodeProjectQuestion') IS NOT NULL
       AND OBJECT_ID(N'dbo.CodeProjectDetails') IS NOT NULL
    BEGIN
        SET @IsCoding = 'CAST(CASE WHEN ISNULL(q.IsCodingQuestion, 0) = 1 OR ISNULL(q.IsCodeProjectQuestion, 0) = 1 THEN 1 ELSE 0 END AS bit)';
        SET @ProjectFilter = N'
      AND (ISNULL(q.IsCodeProjectQuestion, 0) = 0
           OR EXISTS (SELECT 1 FROM CodeProjectDetails cpd WHERE cpd.QuestionId = q.id AND cpd.ValidationStatus = ''passed''))';
    END

    DECLARE @Sql nvarchar(max) = N'
    SELECT q.id,
           LTRIM(RTRIM(q.SkillName)) AS SkillName,
           q.Comment AS Title,
           ' + @MaxTimeMinutes + N' AS MaxTimeMinutes,
           ' + @MaxScore + N' AS MaxScore,
           ' + @Difficulty + N' AS Difficulty,
           ' + @IsCoding + N' AS IsCodingQuestion
    FROM SkillChannelSampleQuestions q
    WHERE q.IRFId = @IRFId
      AND q.SkillName IS NOT NULL
      AND LTRIM(RTRIM(q.SkillName)) <> ''''
      AND q.Comment IS NOT NULL
      AND LTRIM(RTRIM(q.Comment)) <> ''''' + @ProjectFilter + N'
    ORDER BY q.id;';

    EXEC sp_executesql @Sql, N'@IRFId int', @IRFId = @IRFId;
END
GO

/* ---------------------------------------------------------------------
   Verification — every row should report OK.
   --------------------------------------------------------------------- */
SELECT 'SkillChannelSampleQuestions.IsCodeProjectQuestion' AS [Object],
       CASE WHEN EXISTS (SELECT * FROM sys.columns WHERE Name = N'IsCodeProjectQuestion'
                           AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
            THEN 'OK' ELSE 'MISSING' END AS Status
UNION ALL SELECT 'IsCodeProjectQuestion NULLs backfilled',
       CASE WHEN NOT EXISTS (SELECT 1 FROM SkillChannelSampleQuestions WHERE IsCodeProjectQuestion IS NULL)
            THEN 'OK' ELSE 'NULLS REMAIN - admin panel will fail' END
UNION ALL SELECT 'StackImages table',
       CASE WHEN OBJECT_ID(N'StackImages') IS NOT NULL THEN 'OK' ELSE 'MISSING' END
UNION ALL SELECT 'StackImages has an active image',
       CASE WHEN EXISTS (SELECT 1 FROM StackImages WHERE IsActive = 1)
            THEN 'OK' ELSE 'EMPTY - bake an image before the generator can run' END
UNION ALL SELECT 'CodeProjectDetails table',
       CASE WHEN OBJECT_ID(N'CodeProjectDetails') IS NOT NULL THEN 'OK' ELSE 'MISSING' END
UNION ALL SELECT 'CK_CodeProjectDetails_Servable',
       CASE WHEN EXISTS (SELECT * FROM sys.check_constraints WHERE name = N'CK_CodeProjectDetails_Servable')
            THEN 'OK' ELSE 'MISSING' END
UNION ALL SELECT 'CodingAssessment.ResultsJson',
       CASE WHEN EXISTS (SELECT * FROM sys.columns WHERE Name = N'ResultsJson' AND Object_ID = Object_ID(N'CodingAssessment'))
            THEN 'OK' ELSE 'MISSING' END
UNION ALL SELECT 'usp_GetRandomQuestionForSession filters unvalidated projects',
       CASE WHEN OBJECT_DEFINITION(OBJECT_ID(N'usp_GetRandomQuestionForSession')) LIKE '%ValidationStatus%'
            THEN 'OK' ELSE 'STALE - unvalidated projects could reach candidates' END
UNION ALL SELECT 'usp_AddCodeProjectQuestion',
       CASE WHEN OBJECT_ID(N'usp_AddCodeProjectQuestion') IS NOT NULL THEN 'OK' ELSE 'MISSING' END;
GO
