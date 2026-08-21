-- =============================================================================
-- usp_SaveAIQuestionSets -- rejection proofs for the four balance invariants
--
-- The SQL gate in usp_SaveAIQuestionSets has never been executed. This script
-- proves it rejects each of the four ways a set deal can be unbalanced, and
-- that it writes nothing when it does.
--
-- Apply the schema in dependency order before running this:
--   1. Tables\SkillChannelSampleQuestions.sql, Tables\AIQuestionSets.sql,
--      Tables\AIQuestionSetItems.sql, Tables\InterviewAIQuestionSet.sql
--   2. TableTypes\AIQuestionSetItems_TableType.sql
--   3. StoredProcedures\usp_SaveAIQuestionSets.sql
--
-- Run this as a single batch (do not add GO): the transaction, the fixture and
-- every variable have to live for the whole script.
--
-- NON-DESTRUCTIVE. Everything happens inside one outer transaction that is
-- rolled back at the end, and each case is undone by its own savepoint. The
-- fixture is created and destroyed here; no existing row is read for anything
-- except the two foreign keys IRF demands, and none is modified.
--
-- Why savepoints rather than a plain BEGIN TRAN / ROLLBACK per case: an
-- unnamed ROLLBACK discards every open transaction, which would take the
-- fixture with it. Running with a transaction already open is also the
-- @NestedTran path in the procedure, which is the path it documents as the one
-- these tests exercise.
--
-- A rejection for the wrong reason is a FAIL, not a pass, so each case asserts
-- on the message text as well as on @Success. Case 0 is a deliberate positive
-- control: without it, a procedure that refused everything would score five
-- passes.
-- =============================================================================

SET NOCOUNT ON;

-- sqlcmd connects with QUOTED_IDENTIFIER OFF, and the pre-flight below uses an
-- XML data type method, which requires it ON. Set it explicitly so the result
-- does not depend on which client runs the script.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Violations come back as @Success = 0 with a message rather than as errors, so
-- the batch has to stay alive to inspect them.
SET XACT_ABORT OFF;

-- ---------------------------------------------------------------------------
-- Declarations. T-SQL scopes variables to the batch, so they all live here.
-- ---------------------------------------------------------------------------
DECLARE @Results TABLE
(
    Seq         int IDENTITY(1,1) NOT NULL,
    TestCase    varchar(200)      NOT NULL,
    Expectation varchar(200)      NOT NULL,
    Outcome     varchar(4)        NOT NULL,
    Detail      nvarchar(1200)    NULL
);
-- Table variables take no part in transaction rollback, which is exactly what is
-- wanted here: this survives the final ROLLBACK so the report can still be read.

DECLARE @Items dbo.AIQuestionSetItems_TableType;

DECLARE @ClientId          int,
        @RisebirdHr        int,
        @Uncovered         nvarchar(2000),   -- RAISERROR cannot substitute an nvarchar(max)
        @IRFId             int,
        @OtherIRFId        int,
        @SD1               int,
        @SD2               int,
        @SD3               int,
        @C1                int,
        @C2                int,
        @C3                int,
        @SDX               int,
        @Success           bit,
        @Message           nvarchar(1000),
        @Written           int,
        @SetRows           int,
        @ItemRows          int,
        @FirstSkillInSetA  nvarchar(500),
        @MinutesInSetA     int,
        @PointsInSetA      int,
        @Failures          int;

-- ---------------------------------------------------------------------------
-- Pre-flight. IRF is a long-lived table that has grown by ALTER for years, so
-- confirm this script still knows how to build a row in it before starting.
-- ---------------------------------------------------------------------------
SELECT TOP 1 @ClientId = id FROM Client ORDER BY id;
SELECT TOP 1 @RisebirdHr = id FROM UserList ORDER BY id;

IF @ClientId IS NULL OR @RisebirdHr IS NULL
BEGIN
    RAISERROR('Fixture cannot be built: IRF requires a Client row and a UserList row to satisfy FK_IRF_Client and FK_IRF_User, and one of those tables is empty.', 16, 1);
    RETURN;
END

SELECT @Uncovered = STUFF((SELECT ', ' + c.name
                           FROM sys.columns c
                           WHERE c.object_id = OBJECT_ID('dbo.IRF')
                             AND c.is_nullable = 0
                             AND c.is_identity = 0
                             AND c.is_computed = 0
                             AND c.default_object_id = 0
                             AND c.name NOT IN ('IRFId', 'ClientId', 'StartDate', 'EndDate',
                                                'RisebirdHr', 'Discount', 'IsBiometricEnabled',
                                                'PriorityLevel', 'IsPaused', 'IsAIInterview',
                                                'AIInterviewType')
                           ORDER BY c.name
                           FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, '');

IF @Uncovered IS NOT NULL
BEGIN
    RAISERROR('Fixture cannot be built: IRF has NOT NULL columns with no default that this script does not supply (%s). Add them to the fixture INSERT below.', 16, 1, @Uncovered);
    RETURN;
END

BEGIN TRANSACTION RBQSetTests;

BEGIN TRY

    -- -----------------------------------------------------------------------
    -- Fixture.
    --
    -- Two rubric parameters, three questions behind each, so the smallest
    -- per-parameter pool is 3 and the channel therefore supports exactly three
    -- sets. Equal pools are deliberate: they make the valid set count
    -- unambiguous, so a case that supplies a different number can only be
    -- failing I4.
    --
    -- A second channel holds one question, used to prove a question belonging
    -- to another channel is refused.
    -- -----------------------------------------------------------------------
    INSERT INTO IRF (IRFId, ClientId, StartDate, EndDate, RisebirdHr, Discount,
                     IsBiometricEnabled, PriorityLevel, IsPaused,
                     IsAIInterview, AIInterviewType)
    VALUES ('RBTEST-QSET-MAIN', @ClientId, GETDATE(), GETDATE(), @RisebirdHr, 0,
            0, 3, 0,
            1, 2);
    SET @IRFId = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO IRF (IRFId, ClientId, StartDate, EndDate, RisebirdHr, Discount,
                     IsBiometricEnabled, PriorityLevel, IsPaused,
                     IsAIInterview, AIInterviewType)
    VALUES ('RBTEST-QSET-OTHER', @ClientId, GETDATE(), GETDATE(), @RisebirdHr, 0,
            0, 3, 0,
            1, 2);
    SET @OtherIRFId = CAST(SCOPE_IDENTITY() AS int);

    -- Rubric parameters are the distinct TransTechSkill.SkillName values.
    INSERT INTO TransTechSkill (IRFId, SkillName) VALUES (@IRFId, 'System Design');
    INSERT INTO TransTechSkill (IRFId, SkillName) VALUES (@IRFId, 'Concurrency');

    INSERT INTO SkillChannelSampleQuestions (IRFId, Comment, SkillName, CreatedBy)
    VALUES (@IRFId, N'Design a rate limiter.', N'System Design', 'rbtest');
    SET @SD1 = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO SkillChannelSampleQuestions (IRFId, Comment, SkillName, CreatedBy)
    VALUES (@IRFId, N'Design a URL shortener.', N'System Design', 'rbtest');
    SET @SD2 = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO SkillChannelSampleQuestions (IRFId, Comment, SkillName, CreatedBy)
    VALUES (@IRFId, N'Design a notification fan-out.', N'System Design', 'rbtest');
    SET @SD3 = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO SkillChannelSampleQuestions (IRFId, Comment, SkillName, CreatedBy)
    VALUES (@IRFId, N'Explain a data race.', N'Concurrency', 'rbtest');
    SET @C1 = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO SkillChannelSampleQuestions (IRFId, Comment, SkillName, CreatedBy)
    VALUES (@IRFId, N'Explain optimistic locking.', N'Concurrency', 'rbtest');
    SET @C2 = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO SkillChannelSampleQuestions (IRFId, Comment, SkillName, CreatedBy)
    VALUES (@IRFId, N'Explain a deadlock and how to break it.', N'Concurrency', 'rbtest');
    SET @C3 = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO SkillChannelSampleQuestions (IRFId, Comment, SkillName, CreatedBy)
    VALUES (@OtherIRFId, N'A question belonging to another channel.', N'System Design', 'rbtest');
    SET @SDX = CAST(SCOPE_IDENTITY() AS int);

    -- =======================================================================
    -- Case 0 -- positive control: a balanced deal of three sets is accepted.
    --
    -- SortOrder puts System Design first even though Concurrency sorts before
    -- it alphabetically, so the ask-order assertion below fails if the
    -- procedure ever orders the insert by anything other than SortOrder.
    -- =======================================================================
    SAVE TRANSACTION Case0;

    DELETE FROM @Items;
    INSERT INTO @Items (SetName, QuestionId, SkillName, TotalMinutes, TotalPoints, SortOrder) VALUES
        ('A', @SD1, N'System Design', 10, 5, 1),
        ('A', @C1,  N'Concurrency',   10, 5, 2),
        ('B', @SD2, N'System Design', 10, 5, 1),
        ('B', @C2,  N'Concurrency',   10, 5, 2),
        ('C', @SD3, N'System Design', 10, 5, 1),
        ('C', @C3,  N'Concurrency',   10, 5, 2);

    EXEC dbo.usp_SaveAIQuestionSets
        @IRFId         = @IRFId,
        @Items         = @Items,
        @PromptId      = 'pmpt_rbtest',
        @PromptVersion = '1',
        @CreatedBy     = 'rbtest',
        @Success       = @Success OUTPUT,
        @Message       = @Message OUTPUT;

    SET @SetRows  = (SELECT COUNT(*) FROM AIQuestionSets WHERE IRFId = @IRFId);
    SET @ItemRows = (SELECT COUNT(*) FROM AIQuestionSetItems i
                     JOIN AIQuestionSets s ON s.id = i.SetId
                     WHERE s.IRFId = @IRFId);

    SELECT TOP 1 @FirstSkillInSetA = i.SkillName
    FROM AIQuestionSetItems i
    JOIN AIQuestionSets s ON s.id = i.SetId
    WHERE s.IRFId = @IRFId AND s.SetName = 'A'
    ORDER BY i.id;

    SELECT @MinutesInSetA = TotalMinutes, @PointsInSetA = TotalPoints
    FROM AIQuestionSets WHERE IRFId = @IRFId AND SetName = 'A';

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Control -- a balanced deal of 3 sets',
           'accepted and written',
           CASE WHEN @Success = 1
                     AND @Message LIKE '%Created 3 question set(s)%'
                     AND @SetRows = 3
                     AND @ItemRows = 6
                     AND @FirstSkillInSetA = N'System Design'
                     AND @MinutesInSetA = 20
                     AND @PointsInSetA = 10
                THEN 'PASS' ELSE 'FAIL' END,
           N'success=' + CAST(@Success AS varchar(1))
           + N'; sets=' + CAST(@SetRows AS varchar(10))
           + N'; items=' + CAST(@ItemRows AS varchar(10))
           + N'; first asked in A=' + ISNULL(@FirstSkillInSetA, N'(none)')
           + N'; A totals=' + CAST(ISNULL(@MinutesInSetA, -1) AS varchar(10))
           + N'/' + CAST(ISNULL(@PointsInSetA, -1) AS varchar(10))
           + N'; ' + ISNULL(@Message, N'(no message)');

    ROLLBACK TRANSACTION Case0;

    -- =======================================================================
    -- Case 1 -- I3: the same question id placed in two sets.
    -- Two papers sharing a question is the leak the whole feature prevents.
    -- =======================================================================
    SAVE TRANSACTION Case1;

    DELETE FROM @Items;
    INSERT INTO @Items (SetName, QuestionId, SkillName, TotalMinutes, TotalPoints, SortOrder) VALUES
        ('A', @SD1, N'System Design', 10, 5, 1),
        ('A', @C1,  N'Concurrency',   10, 5, 2),
        ('B', @SD1, N'System Design', 10, 5, 1),   -- SD1 again
        ('B', @C2,  N'Concurrency',   10, 5, 2),
        ('C', @SD3, N'System Design', 10, 5, 1),
        ('C', @C3,  N'Concurrency',   10, 5, 2);

    EXEC dbo.usp_SaveAIQuestionSets
        @IRFId         = @IRFId,
        @Items         = @Items,
        @PromptId      = 'pmpt_rbtest',
        @PromptVersion = '1',
        @CreatedBy     = 'rbtest',
        @Success       = @Success OUTPUT,
        @Message       = @Message OUTPUT;

    SET @Written = (SELECT COUNT(*) FROM AIQuestionSets WHERE IRFId = @IRFId)
                 + (SELECT COUNT(*) FROM AIQuestionSetItems i
                    JOIN AIQuestionSets s ON s.id = i.SetId
                    WHERE s.IRFId = @IRFId);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'I3 -- same question id in two sets',
           'rejected, nothing written',
           CASE WHEN @Success = 0
                     AND @Message LIKE '%same question was placed in more than one set%'
                     AND @Written = 0
                THEN 'PASS' ELSE 'FAIL' END,
           N'rows written=' + CAST(@Written AS varchar(10))
           + N'; ' + ISNULL(@Message, N'(no message)');

    ROLLBACK TRANSACTION Case1;

    -- =======================================================================
    -- Case 1B -- I2: one set holding two questions of the same parameter.
    --
    -- Not one of the four cases the hand-off lists, but I2 is one of the four
    -- invariants, so it is proved here too. Set A doubles up on System Design
    -- without reusing any question, which is what separates I2 from I3.
    -- =======================================================================
    SAVE TRANSACTION Case1B;

    DELETE FROM @Items;
    INSERT INTO @Items (SetName, QuestionId, SkillName, TotalMinutes, TotalPoints, SortOrder) VALUES
        ('A', @SD1, N'System Design', 10, 5, 1),
        ('A', @SD2, N'System Design', 10, 5, 2),   -- second System Design in A
        ('A', @C1,  N'Concurrency',   10, 5, 3),
        ('B', @SD3, N'System Design', 10, 5, 1),
        ('B', @C2,  N'Concurrency',   10, 5, 2),
        ('C', @C3,  N'Concurrency',   10, 5, 1);

    EXEC dbo.usp_SaveAIQuestionSets
        @IRFId         = @IRFId,
        @Items         = @Items,
        @PromptId      = 'pmpt_rbtest',
        @PromptVersion = '1',
        @CreatedBy     = 'rbtest',
        @Success       = @Success OUTPUT,
        @Message       = @Message OUTPUT;

    SET @Written = (SELECT COUNT(*) FROM AIQuestionSets WHERE IRFId = @IRFId)
                 + (SELECT COUNT(*) FROM AIQuestionSetItems i
                    JOIN AIQuestionSets s ON s.id = i.SetId
                    WHERE s.IRFId = @IRFId);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'I2 -- two questions of one parameter in a set',
           'rejected naming set and parameter',
           CASE WHEN @Success = 0
                     AND @Message LIKE '%Set A holds more than one question for rubric parameter "System Design"%'
                     AND @Written = 0
                THEN 'PASS' ELSE 'FAIL' END,
           N'rows written=' + CAST(@Written AS varchar(10))
           + N'; ' + ISNULL(@Message, N'(no message)');

    ROLLBACK TRANSACTION Case1B;

    -- =======================================================================
    -- Case 2 -- I1: a set missing a rubric parameter.
    -- Set C covers System Design but has no Concurrency question, so a
    -- candidate drawing C would be scored on a different rubric.
    -- =======================================================================
    SAVE TRANSACTION Case2;

    DELETE FROM @Items;
    INSERT INTO @Items (SetName, QuestionId, SkillName, TotalMinutes, TotalPoints, SortOrder) VALUES
        ('A', @SD1, N'System Design', 10, 5, 1),
        ('A', @C1,  N'Concurrency',   10, 5, 2),
        ('B', @SD2, N'System Design', 10, 5, 1),
        ('B', @C2,  N'Concurrency',   10, 5, 2),
        ('C', @SD3, N'System Design', 10, 5, 1);   -- no Concurrency question

    EXEC dbo.usp_SaveAIQuestionSets
        @IRFId         = @IRFId,
        @Items         = @Items,
        @PromptId      = 'pmpt_rbtest',
        @PromptVersion = '1',
        @CreatedBy     = 'rbtest',
        @Success       = @Success OUTPUT,
        @Message       = @Message OUTPUT;

    SET @Written = (SELECT COUNT(*) FROM AIQuestionSets WHERE IRFId = @IRFId)
                 + (SELECT COUNT(*) FROM AIQuestionSetItems i
                    JOIN AIQuestionSets s ON s.id = i.SetId
                    WHERE s.IRFId = @IRFId);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'I1 -- a set missing a rubric parameter',
           'rejected naming set and parameter',
           CASE WHEN @Success = 0
                     AND @Message LIKE '%Set C has no question for rubric parameter "Concurrency"%'
                     AND @Written = 0
                THEN 'PASS' ELSE 'FAIL' END,
           N'rows written=' + CAST(@Written AS varchar(10))
           + N'; ' + ISNULL(@Message, N'(no message)');

    ROLLBACK TRANSACTION Case2;

    -- =======================================================================
    -- Case 3 -- a question id belonging to a different channel.
    -- =======================================================================
    SAVE TRANSACTION Case3;

    DELETE FROM @Items;
    INSERT INTO @Items (SetName, QuestionId, SkillName, TotalMinutes, TotalPoints, SortOrder) VALUES
        ('A', @SD1, N'System Design', 10, 5, 1),
        ('A', @C1,  N'Concurrency',   10, 5, 2),
        ('B', @SD2, N'System Design', 10, 5, 1),
        ('B', @C2,  N'Concurrency',   10, 5, 2),
        ('C', @SDX, N'System Design', 10, 5, 1),   -- belongs to the other channel
        ('C', @C3,  N'Concurrency',   10, 5, 2);

    EXEC dbo.usp_SaveAIQuestionSets
        @IRFId         = @IRFId,
        @Items         = @Items,
        @PromptId      = 'pmpt_rbtest',
        @PromptVersion = '1',
        @CreatedBy     = 'rbtest',
        @Success       = @Success OUTPUT,
        @Message       = @Message OUTPUT;

    SET @Written = (SELECT COUNT(*) FROM AIQuestionSets WHERE IRFId = @IRFId)
                 + (SELECT COUNT(*) FROM AIQuestionSetItems i
                    JOIN AIQuestionSets s ON s.id = i.SetId
                    WHERE s.IRFId = @IRFId);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Question belonging to a different channel',
           'rejected, nothing written',
           CASE WHEN @Success = 0
                     AND @Message LIKE '%do not exist or belong to a different skill channel%'
                     AND @Written = 0
                THEN 'PASS' ELSE 'FAIL' END,
           N'rows written=' + CAST(@Written AS varchar(10))
           + N'; ' + ISNULL(@Message, N'(no message)');

    ROLLBACK TRANSACTION Case3;

    -- =======================================================================
    -- Case 4 -- I4: a set count that is not the smallest per-parameter pool.
    --
    -- Two complete sets against a pool that supports three. Under-supplying is
    -- what isolates I4: asking for more sets than the pool allows necessarily
    -- leaves a set short of a parameter, so I1 would reject it first and I4
    -- would never be reached.
    -- =======================================================================
    SAVE TRANSACTION Case4;

    DELETE FROM @Items;
    INSERT INTO @Items (SetName, QuestionId, SkillName, TotalMinutes, TotalPoints, SortOrder) VALUES
        ('A', @SD1, N'System Design', 10, 5, 1),
        ('A', @C1,  N'Concurrency',   10, 5, 2),
        ('B', @SD2, N'System Design', 10, 5, 1),
        ('B', @C2,  N'Concurrency',   10, 5, 2);

    EXEC dbo.usp_SaveAIQuestionSets
        @IRFId         = @IRFId,
        @Items         = @Items,
        @PromptId      = 'pmpt_rbtest',
        @PromptVersion = '1',
        @CreatedBy     = 'rbtest',
        @Success       = @Success OUTPUT,
        @Message       = @Message OUTPUT;

    SET @Written = (SELECT COUNT(*) FROM AIQuestionSets WHERE IRFId = @IRFId)
                 + (SELECT COUNT(*) FROM AIQuestionSetItems i
                    JOIN AIQuestionSets s ON s.id = i.SetId
                    WHERE s.IRFId = @IRFId);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'I4 -- set count is not the smallest pool',
           'rejected naming the supported count',
           CASE WHEN @Success = 0
                     AND @Message LIKE '%supports exactly 3 set(s)%'
                     AND @Message LIKE '%2 set(s) were supplied%'
                     AND @Written = 0
                THEN 'PASS' ELSE 'FAIL' END,
           N'rows written=' + CAST(@Written AS varchar(10))
           + N'; ' + ISNULL(@Message, N'(no message)');

    ROLLBACK TRANSACTION Case4;

    ROLLBACK TRANSACTION RBQSetTests;

END TRY
BEGIN CATCH

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    VALUES ('Harness', 'no unexpected error', 'FAIL',
            N'Error ' + CAST(ERROR_NUMBER() AS varchar(10))
            + N' at line ' + CAST(ERROR_LINE() AS varchar(10))
            + N': ' + ERROR_MESSAGE());

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

END CATCH

-- ---------------------------------------------------------------------------
-- The fixture was created inside the rolled-back transaction, so it must be
-- gone. Proving that here is what makes the script safe to run more than once.
-- ---------------------------------------------------------------------------
INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
SELECT 'Fixture cleanup',
       'no test rows survive the rollback',
       CASE WHEN NOT EXISTS (SELECT 1 FROM IRF
                             WHERE IRFId IN ('RBTEST-QSET-MAIN', 'RBTEST-QSET-OTHER'))
            THEN 'PASS' ELSE 'FAIL' END,
       N'open transactions left behind=' + CAST(@@TRANCOUNT AS varchar(10));

SELECT Seq, TestCase, Expectation, Outcome, Detail FROM @Results ORDER BY Seq;

SET @Failures = (SELECT COUNT(*) FROM @Results WHERE Outcome = 'FAIL');

IF @Failures = 0
    SELECT 'ALL PASS -- the SQL half of the balance gate rejects every unbalanced deal and writes nothing. The C# half (ValidateAIQuestionSets) is not exercised here.' AS Verdict;
ELSE
    SELECT CAST(@Failures AS varchar(10)) + ' FAILED -- see the rows above. Do not use question sets with real candidates until these pass.' AS Verdict;
