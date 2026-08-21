-- =============================================================================
-- IRF.UseAIQuestionSets -- proofs for the explicit adoption flag
--
-- Adoption of question sets used to be inferred from the presence of rows in
-- AIQuestionSets, which cannot tell "this channel deliberately does not deal
-- papers" from "set creation never succeeded here". The second of those quietly
-- serves every candidate in a batch the same questions, which is the one thing
-- the sets exist to prevent. It is now an explicit per-channel flag.
--
-- This proves the four states the runtime has to tell apart, that an interview
-- already bound keeps its paper when the channel is switched off, and that
-- usp_SetAIQuestionSetsEnabled refuses to create the dangerous state.
--
-- Apply the schema in dependency order before running this:
--   1. Tables\IRF.sql (which adds UseAIQuestionSets), Tables\TransTechSkill.sql,
--      Tables\SkillChannelSampleQuestions.sql, Tables\AIQuestionSets.sql,
--      Tables\AIQuestionSetItems.sql, Tables\InterviewAIQuestionSet.sql
--   2. StoredProcedures\usp_AssignAIQuestionSetToInterview.sql
--   3. StoredProcedures\usp_SetAIQuestionSetsEnabled.sql
--
-- Run this as a single batch (do not add GO): the transaction, the fixture and
-- every variable have to live for the whole script.
--
-- NON-DESTRUCTIVE. Everything happens inside one outer transaction that is
-- rolled back at the end. Nothing existing is read except the two foreign keys
-- IRF demands, and nothing existing is modified.
--
-- Running with a transaction already open is also the @NestedTran path in
-- usp_AssignAIQuestionSetToInterview, which is the path a booking flow takes.
-- =============================================================================

SET NOCOUNT ON;

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- The procedure reports refusals through output parameters rather than errors,
-- so the batch has to stay alive to inspect them.
SET XACT_ABORT OFF;

DECLARE @Results TABLE
(
    Seq         int IDENTITY(1,1) NOT NULL,
    TestCase    varchar(200)      NOT NULL,
    Expectation varchar(200)      NOT NULL,
    Outcome     varchar(4)        NOT NULL,
    Detail      nvarchar(1200)    NULL
);
-- Table variables take no part in transaction rollback, so this survives the
-- final ROLLBACK and the report can still be read.

DECLARE @ClientId       int,
        @RisebirdHr     int,
        @IRFId          int,
        @PlainIRFId     int,
        @IRFCode        varchar(200) = 'RBTEST-QSETFLAG-MAIN',
        @PlainCode      varchar(200) = 'RBTEST-QSETFLAG-PLAIN',
        @SetA           int,
        @SetB           int,
        @Q1             int,
        @Q2             int,
        @InterviewId    int,
        @Interview2Id   int,
        @PlainIntId     int,
        @SetId          int,
        @ChannelHasSets bit,
        @SetsEnabled    bit,
        @Message        nvarchar(400),
        @FirstBoundSet  int,
        @Bindings       int,
        @Success        bit,
        @EnabledResult  bit,
        @FlagNow        bit,
        @Failures       int;

SELECT TOP 1 @ClientId   = id FROM Client   ORDER BY id;
SELECT TOP 1 @RisebirdHr = id FROM UserList ORDER BY id;

IF @ClientId IS NULL OR @RisebirdHr IS NULL
BEGIN
    RAISERROR('Fixture cannot be built: IRF requires a Client row and a UserList row to satisfy FK_IRF_Client and FK_IRF_User, and one of those tables is empty.', 16, 1);
    RETURN;
END

-- The whole point of this script is the new column, so say so plainly rather
-- than failing on an opaque error inside the procedure.
IF COL_LENGTH('dbo.IRF', 'UseAIQuestionSets') IS NULL
BEGIN
    RAISERROR('IRF.UseAIQuestionSets does not exist. Apply Tables\IRF.sql before running these proofs.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION RBQSetFlagTests;

BEGIN TRY

    -- -----------------------------------------------------------------------
    -- Fixture. One preset channel with two papers, and a second preset channel
    -- with none, so both halves of "switched on" can be exercised. A third
    -- interview sits on a non-preset channel.
    --
    -- InterviewMeetings.IRFId holds the channel's business code, not IRF.id,
    -- which is the join the procedure performs.
    -- -----------------------------------------------------------------------
    INSERT INTO IRF (IRFId, ClientId, StartDate, EndDate, RisebirdHr, Discount,
                     IsBiometricEnabled, PriorityLevel, IsPaused,
                     IsAIInterview, AIInterviewType, UseAIQuestionSets)
    VALUES (@IRFCode, @ClientId, GETDATE(), GETDATE(), @RisebirdHr, 0,
            0, 3, 0,
            1, 2, NULL);
    SET @IRFId = CAST(SCOPE_IDENTITY() AS int);

    -- Not an AI interview channel at all.
    INSERT INTO IRF (IRFId, ClientId, StartDate, EndDate, RisebirdHr, Discount,
                     IsBiometricEnabled, PriorityLevel, IsPaused,
                     IsAIInterview, AIInterviewType, UseAIQuestionSets)
    VALUES (@PlainCode, @ClientId, GETDATE(), GETDATE(), @RisebirdHr, 0,
            0, 3, 0,
            0, 0, NULL);
    SET @PlainIRFId = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO TransTechSkill (IRFId, SkillName) VALUES (@IRFId, 'System Design');

    INSERT INTO SkillChannelSampleQuestions (IRFId, Comment, SkillName, CreatedBy)
    VALUES (@IRFId, N'Design a rate limiter.', N'System Design', 'rbtest');
    SET @Q1 = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO SkillChannelSampleQuestions (IRFId, Comment, SkillName, CreatedBy)
    VALUES (@IRFId, N'Design a URL shortener.', N'System Design', 'rbtest');
    SET @Q2 = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO InterviewMeetings (IRFId, SessionId) VALUES (@IRFCode, NEWID());
    SET @InterviewId = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO InterviewMeetings (IRFId, SessionId) VALUES (@IRFCode, NEWID());
    SET @Interview2Id = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO InterviewMeetings (IRFId, SessionId) VALUES (@PlainCode, NEWID());
    SET @PlainIntId = CAST(SCOPE_IDENTITY() AS int);

    -- =======================================================================
    -- Case 1 -- flag NULL, no sets. The pre-feature state of every channel in
    -- the database, and it must stay on the pre-feature path.
    -- =======================================================================
    EXEC dbo.usp_AssignAIQuestionSetToInterview
        @InterviewId    = @InterviewId,
        @SetId          = @SetId          OUTPUT,
        @ChannelHasSets = @ChannelHasSets OUTPUT,
        @SetsEnabled    = @SetsEnabled    OUTPUT,
        @Message        = @Message        OUTPUT;

    SET @Bindings = (SELECT COUNT(*) FROM InterviewAIQuestionSet WHERE InterviewId = @InterviewId);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Not switched on, no sets',
           'no binding, reported as not using sets',
           CASE WHEN @SetId IS NULL AND @ChannelHasSets = 0 AND @SetsEnabled = 0 AND @Bindings = 0
                THEN 'PASS' ELSE 'FAIL' END,
           N'setId=' + ISNULL(CAST(@SetId AS varchar(10)), N'(null)')
           + N'; hasSets=' + CAST(@ChannelHasSets AS varchar(1))
           + N'; enabled=' + CAST(@SetsEnabled AS varchar(1))
           + N'; bindings=' + CAST(@Bindings AS varchar(10))
           + N'; ' + ISNULL(@Message, N'(no message)');

    -- =======================================================================
    -- Case 2 -- switched ON with no sets. The misconfiguration. It must report
    -- enabled with no sets so the caller can refuse; falling back to the whole
    -- channel pool here would hand every candidate the same questions.
    -- =======================================================================
    UPDATE IRF SET UseAIQuestionSets = 1 WHERE id = @IRFId;

    EXEC dbo.usp_AssignAIQuestionSetToInterview
        @InterviewId    = @InterviewId,
        @SetId          = @SetId          OUTPUT,
        @ChannelHasSets = @ChannelHasSets OUTPUT,
        @SetsEnabled    = @SetsEnabled    OUTPUT,
        @Message        = @Message        OUTPUT;

    SET @Bindings = (SELECT COUNT(*) FROM InterviewAIQuestionSet WHERE InterviewId = @InterviewId);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Switched on, no sets',
           'enabled reported with no sets, nothing bound',
           CASE WHEN @SetId IS NULL AND @ChannelHasSets = 0 AND @SetsEnabled = 1
                     AND @Bindings = 0
                     AND @Message LIKE '%switched on%none have been created%'
                THEN 'PASS' ELSE 'FAIL' END,
           N'setId=' + ISNULL(CAST(@SetId AS varchar(10)), N'(null)')
           + N'; hasSets=' + CAST(@ChannelHasSets AS varchar(1))
           + N'; enabled=' + CAST(@SetsEnabled AS varchar(1))
           + N'; bindings=' + CAST(@Bindings AS varchar(10))
           + N'; ' + ISNULL(@Message, N'(no message)');

    -- Two papers, one question each, from here on.
    INSERT INTO AIQuestionSets (IRFId, SetName, TotalMinutes, TotalPoints, PromptId, PromptVersion, CreatedBy, CreatedOn)
    VALUES (@IRFId, 'A', 10, 5, 'pmpt_rbtest', '1', 'rbtest', GETDATE());
    SET @SetA = CAST(SCOPE_IDENTITY() AS int);
    INSERT INTO AIQuestionSetItems (SetId, QuestionId, SkillName) VALUES (@SetA, @Q1, N'System Design');

    INSERT INTO AIQuestionSets (IRFId, SetName, TotalMinutes, TotalPoints, PromptId, PromptVersion, CreatedBy, CreatedOn)
    VALUES (@IRFId, 'B', 10, 5, 'pmpt_rbtest', '1', 'rbtest', GETDATE());
    SET @SetB = CAST(SCOPE_IDENTITY() AS int);
    INSERT INTO AIQuestionSetItems (SetId, QuestionId, SkillName) VALUES (@SetB, @Q2, N'System Design');

    -- =======================================================================
    -- Case 3 -- switched OFF with sets present. A legitimate state: papers are
    -- created first and switched on second. It must take the pre-feature path
    -- and bind nothing, while still reporting that the papers exist so the
    -- caller can warn about a channel doing the work for no benefit.
    -- =======================================================================
    UPDATE IRF SET UseAIQuestionSets = 0 WHERE id = @IRFId;

    EXEC dbo.usp_AssignAIQuestionSetToInterview
        @InterviewId    = @InterviewId,
        @SetId          = @SetId          OUTPUT,
        @ChannelHasSets = @ChannelHasSets OUTPUT,
        @SetsEnabled    = @SetsEnabled    OUTPUT,
        @Message        = @Message        OUTPUT;

    SET @Bindings = (SELECT COUNT(*) FROM InterviewAIQuestionSet WHERE InterviewId = @InterviewId);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Sets present, switched off',
           'sets reported, nothing bound',
           CASE WHEN @SetId IS NULL AND @ChannelHasSets = 1 AND @SetsEnabled = 0 AND @Bindings = 0
                THEN 'PASS' ELSE 'FAIL' END,
           N'setId=' + ISNULL(CAST(@SetId AS varchar(10)), N'(null)')
           + N'; hasSets=' + CAST(@ChannelHasSets AS varchar(1))
           + N'; enabled=' + CAST(@SetsEnabled AS varchar(1))
           + N'; bindings=' + CAST(@Bindings AS varchar(10))
           + N'; ' + ISNULL(@Message, N'(no message)');

    -- =======================================================================
    -- Case 4 -- switched ON with sets. The working state.
    -- =======================================================================
    UPDATE IRF SET UseAIQuestionSets = 1 WHERE id = @IRFId;

    EXEC dbo.usp_AssignAIQuestionSetToInterview
        @InterviewId    = @InterviewId,
        @SetId          = @SetId          OUTPUT,
        @ChannelHasSets = @ChannelHasSets OUTPUT,
        @SetsEnabled    = @SetsEnabled    OUTPUT,
        @Message        = @Message        OUTPUT;

    SET @FirstBoundSet = @SetId;
    SET @Bindings = (SELECT COUNT(*) FROM InterviewAIQuestionSet WHERE InterviewId = @InterviewId);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Switched on, sets present',
           'one paper bound',
           CASE WHEN @SetId IN (@SetA, @SetB) AND @ChannelHasSets = 1 AND @SetsEnabled = 1
                     AND @Bindings = 1
                THEN 'PASS' ELSE 'FAIL' END,
           N'setId=' + ISNULL(CAST(@SetId AS varchar(10)), N'(null)')
           + N'; bindings=' + CAST(@Bindings AS varchar(10))
           + N'; ' + ISNULL(@Message, N'(no message)');

    -- =======================================================================
    -- Case 5 -- called again. Reconnect and resume must land on the same paper.
    -- =======================================================================
    EXEC dbo.usp_AssignAIQuestionSetToInterview
        @InterviewId    = @InterviewId,
        @SetId          = @SetId          OUTPUT,
        @ChannelHasSets = @ChannelHasSets OUTPUT,
        @SetsEnabled    = @SetsEnabled    OUTPUT,
        @Message        = @Message        OUTPUT;

    SET @Bindings = (SELECT COUNT(*) FROM InterviewAIQuestionSet WHERE InterviewId = @InterviewId);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Called twice for one interview',
           'same paper, still one binding',
           CASE WHEN @SetId = @FirstBoundSet AND @Bindings = 1 AND @Message = 'Already bound.'
                THEN 'PASS' ELSE 'FAIL' END,
           N'first=' + ISNULL(CAST(@FirstBoundSet AS varchar(10)), N'(null)')
           + N'; second=' + ISNULL(CAST(@SetId AS varchar(10)), N'(null)')
           + N'; bindings=' + CAST(@Bindings AS varchar(10))
           + N'; ' + ISNULL(@Message, N'(no message)');

    -- =======================================================================
    -- Case 6 -- the channel is switched off while this interview is mid-flight.
    -- An interview is locked to its paper for life, so it must keep the same
    -- one. Moving it back to the standard question order would change the
    -- questions under a candidate who has already answered some of them.
    -- =======================================================================
    UPDATE IRF SET UseAIQuestionSets = 0 WHERE id = @IRFId;

    EXEC dbo.usp_AssignAIQuestionSetToInterview
        @InterviewId    = @InterviewId,
        @SetId          = @SetId          OUTPUT,
        @ChannelHasSets = @ChannelHasSets OUTPUT,
        @SetsEnabled    = @SetsEnabled    OUTPUT,
        @Message        = @Message        OUTPUT;

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Channel switched off after binding',
           'bound interview keeps its paper',
           CASE WHEN @SetId = @FirstBoundSet AND @SetsEnabled = 0
                THEN 'PASS' ELSE 'FAIL' END,
           N'setId=' + ISNULL(CAST(@SetId AS varchar(10)), N'(null)')
           + N'; enabled=' + CAST(@SetsEnabled AS varchar(1))
           + N'; ' + ISNULL(@Message, N'(no message)');

    -- =======================================================================
    -- Case 7 -- a second, unbound interview on the same switched-off channel
    -- must NOT be bound. Case 6 is about honouring history, not about ignoring
    -- the flag.
    -- =======================================================================
    EXEC dbo.usp_AssignAIQuestionSetToInterview
        @InterviewId    = @Interview2Id,
        @SetId          = @SetId          OUTPUT,
        @ChannelHasSets = @ChannelHasSets OUTPUT,
        @SetsEnabled    = @SetsEnabled    OUTPUT,
        @Message        = @Message        OUTPUT;

    SET @Bindings = (SELECT COUNT(*) FROM InterviewAIQuestionSet WHERE InterviewId = @Interview2Id);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'New interview on a switched-off channel',
           'not bound',
           CASE WHEN @SetId IS NULL AND @Bindings = 0 AND @ChannelHasSets = 1
                THEN 'PASS' ELSE 'FAIL' END,
           N'setId=' + ISNULL(CAST(@SetId AS varchar(10)), N'(null)')
           + N'; bindings=' + CAST(@Bindings AS varchar(10))
           + N'; ' + ISNULL(@Message, N'(no message)');

    -- =======================================================================
    -- Case 8 -- a non-preset channel is refused outright, whatever the flag
    -- says. Guards the promise that non-preset channels are untouched.
    -- =======================================================================
    UPDATE IRF SET UseAIQuestionSets = 1 WHERE id = @PlainIRFId;

    EXEC dbo.usp_AssignAIQuestionSetToInterview
        @InterviewId    = @PlainIntId,
        @SetId          = @SetId          OUTPUT,
        @ChannelHasSets = @ChannelHasSets OUTPUT,
        @SetsEnabled    = @SetsEnabled    OUTPUT,
        @Message        = @Message        OUTPUT;

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Non-preset channel, flag forced on',
           'refused, flag ignored',
           CASE WHEN @SetId IS NULL AND @ChannelHasSets = 0 AND @SetsEnabled = 0
                     AND @Message LIKE '%not on a preset AI interview channel%'
                THEN 'PASS' ELSE 'FAIL' END,
           N'setId=' + ISNULL(CAST(@SetId AS varchar(10)), N'(null)')
           + N'; enabled=' + CAST(@SetsEnabled AS varchar(1))
           + N'; ' + ISNULL(@Message, N'(no message)');

    -- =======================================================================
    -- Case 9 -- usp_SetAIQuestionSetsEnabled refuses to switch a channel on
    -- before it has papers, so the dangerous state cannot be reached from the
    -- Delivery Manager at all.
    -- =======================================================================
    DELETE FROM AIQuestionSetItems WHERE SetId IN (@SetA, @SetB);
    DELETE FROM InterviewAIQuestionSet WHERE SetId IN (@SetA, @SetB);
    DELETE FROM AIQuestionSets WHERE id IN (@SetA, @SetB);
    UPDATE IRF SET UseAIQuestionSets = 0 WHERE id = @IRFId;

    EXEC dbo.usp_SetAIQuestionSetsEnabled
        @IRFId          = @IRFId,
        @Enabled        = 1,
        @Success        = @Success       OUTPUT,
        @Enabled_Result = @EnabledResult OUTPUT,
        @Message        = @Message       OUTPUT;

    SET @FlagNow = (SELECT ISNULL(UseAIQuestionSets, 0) FROM IRF WHERE id = @IRFId);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Switch on a channel with no sets',
           'refused, flag unchanged',
           CASE WHEN @Success = 0 AND @EnabledResult = 0 AND @FlagNow = 0
                     AND @Message LIKE '%Create the question sets first%'
                THEN 'PASS' ELSE 'FAIL' END,
           N'success=' + CAST(@Success AS varchar(1))
           + N'; flag now=' + CAST(@FlagNow AS varchar(1))
           + N'; ' + ISNULL(@Message, N'(no message)');

    -- =======================================================================
    -- Case 10 -- positive control for the toggle. Without it, a procedure that
    -- refused everything would score a pass on case 9.
    -- =======================================================================
    INSERT INTO AIQuestionSets (IRFId, SetName, TotalMinutes, TotalPoints, PromptId, PromptVersion, CreatedBy, CreatedOn)
    VALUES (@IRFId, 'A', 10, 5, 'pmpt_rbtest', '1', 'rbtest', GETDATE());
    SET @SetA = CAST(SCOPE_IDENTITY() AS int);
    INSERT INTO AIQuestionSetItems (SetId, QuestionId, SkillName) VALUES (@SetA, @Q1, N'System Design');

    EXEC dbo.usp_SetAIQuestionSetsEnabled
        @IRFId          = @IRFId,
        @Enabled        = 1,
        @Success        = @Success       OUTPUT,
        @Enabled_Result = @EnabledResult OUTPUT,
        @Message        = @Message       OUTPUT;

    SET @FlagNow = (SELECT ISNULL(UseAIQuestionSets, 0) FROM IRF WHERE id = @IRFId);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Control -- switch on a channel that has sets',
           'accepted and persisted',
           CASE WHEN @Success = 1 AND @EnabledResult = 1 AND @FlagNow = 1
                THEN 'PASS' ELSE 'FAIL' END,
           N'success=' + CAST(@Success AS varchar(1))
           + N'; flag now=' + CAST(@FlagNow AS varchar(1))
           + N'; ' + ISNULL(@Message, N'(no message)');

    -- =======================================================================
    -- Case 11 -- the backfill. A channel that already deals papers and has
    -- never seen the flag must come out switched ON, or shipping this column
    -- silently reverts every adopted channel to the standard question order.
    -- =======================================================================
    UPDATE IRF SET UseAIQuestionSets = NULL WHERE id = @IRFId;

    UPDATE irf
    SET UseAIQuestionSets = 1
    FROM IRF irf
    WHERE irf.UseAIQuestionSets IS NULL
      AND EXISTS (SELECT 1 FROM AIQuestionSets s WHERE s.IRFId = irf.id);

    SET @FlagNow = (SELECT ISNULL(UseAIQuestionSets, 0) FROM IRF WHERE id = @IRFId);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Backfill for a channel that already has sets',
           'switched on',
           CASE WHEN @FlagNow = 1 THEN 'PASS' ELSE 'FAIL' END,
           N'flag now=' + CAST(@FlagNow AS varchar(1));

    -- =======================================================================
    -- Case 12 -- the backfill must not switch on a channel that was switched
    -- off on purpose, however often it is re-run.
    -- =======================================================================
    UPDATE IRF SET UseAIQuestionSets = 0 WHERE id = @IRFId;

    UPDATE irf
    SET UseAIQuestionSets = 1
    FROM IRF irf
    WHERE irf.UseAIQuestionSets IS NULL
      AND EXISTS (SELECT 1 FROM AIQuestionSets s WHERE s.IRFId = irf.id);

    SET @FlagNow = (SELECT ISNULL(UseAIQuestionSets, 0) FROM IRF WHERE id = @IRFId);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Backfill re-run over a deliberate off',
           'left switched off',
           CASE WHEN @FlagNow = 0 THEN 'PASS' ELSE 'FAIL' END,
           N'flag now=' + CAST(@FlagNow AS varchar(1));

    ROLLBACK TRANSACTION RBQSetFlagTests;

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
       CASE WHEN NOT EXISTS (SELECT 1 FROM IRF WHERE IRFId IN (@IRFCode, @PlainCode))
                 AND NOT EXISTS (SELECT 1 FROM InterviewMeetings WHERE IRFId IN (@IRFCode, @PlainCode))
            THEN 'PASS' ELSE 'FAIL' END,
       N'open transactions left behind=' + CAST(@@TRANCOUNT AS varchar(10));

SELECT Seq, TestCase, Expectation, Outcome, Detail FROM @Results ORDER BY Seq;

SET @Failures = (SELECT COUNT(*) FROM @Results WHERE Outcome = 'FAIL');

IF @Failures = 0
    SELECT 'ALL PASS -- adoption is an explicit flag, a switched-on channel with no sets is reported rather than silently falling back, and an interview keeps its paper for life.' AS Verdict;
ELSE
    SELECT CAST(@Failures AS varchar(10)) + ' FAILED -- see the rows above. Do not ship the flag until these pass.' AS Verdict;
