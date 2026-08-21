-- =============================================================================
-- usp_DeleteSkillSampleQuestions -- proofs for the question set guard
--
-- Deleting a sample question used to be unconditional, and nothing referenced it
-- back: AIQuestionSetItems carries no foreign key to SkillChannelSampleQuestions,
-- and usp_GetNextAIQuestionForInterview inner-joins the question table. A deleted
-- question therefore disappeared from every paper holding it, silently, and the
-- interview asked fewer questions than the rubric has parameters.
--
-- This proves the three outcomes the procedure now has to distinguish, that the
-- refusal is scoped to the channel rather than to one set, and that a channel
-- whose papers were never used can still have its pool edited.
--
-- Apply the schema in dependency order before running this:
--   1. Tables\IRF.sql, Tables\TransTechSkill.sql,
--      Tables\SkillChannelSampleQuestions.sql, Tables\AIQuestionSets.sql,
--      Tables\AIQuestionSetItems.sql, Tables\InterviewAIQuestionSet.sql
--   2. StoredProcedures\usp_DeleteSkillSampleQuestions.sql
--
-- Run this as a single batch (do not add GO): the transaction, the fixture and
-- every variable have to live for the whole script.
--
-- NON-DESTRUCTIVE. Everything happens inside one outer transaction that is
-- rolled back at the end. Running with a transaction already open is also the
-- @NestedTran path inside the procedure, which is the path it takes here.
-- =============================================================================

SET NOCOUNT ON;

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- The procedure reports refusals through a result set rather than an error, so
-- the batch has to stay alive to inspect them.
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

DECLARE @Outcome TABLE
(
    Success       bit            NULL,
    [Message]     nvarchar(1000) NULL,
    SetsDiscarded int            NULL
);

DECLARE @ClientId      int,
        @RisebirdHr    int,
        @IRFId         int,
        @OtherIRFId    int,
        @IRFCode       varchar(200) = 'RBTEST-QDEL-MAIN',
        @OtherCode     varchar(200) = 'RBTEST-QDEL-OTHER',
        @SetA          int,
        @SetB          int,
        @OtherSet      int,
        @Q1            int,
        @Q2            int,
        @Q3            int,
        @Q4            int,
        @Loose         int,
        @OtherQ        int,
        @InterviewId   int,
        @Success       bit,
        @Message       nvarchar(1000),
        @Discarded     int,
        @MainSets      int,
        @OtherSets     int,
        @Items         int,
        @QuestionsLeft int,
        @Bindings      int,
        @Failures      int;

SELECT TOP 1 @ClientId   = id FROM Client   ORDER BY id;
SELECT TOP 1 @RisebirdHr = id FROM UserList ORDER BY id;

IF @ClientId IS NULL OR @RisebirdHr IS NULL
BEGIN
    RAISERROR('Fixture cannot be built: IRF requires a Client row and a UserList row to satisfy FK_IRF_Client and FK_IRF_User, and one of those tables is empty.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION RBQDelTests;

BEGIN TRY

    -- -----------------------------------------------------------------------
    -- Fixture. One preset channel with two rubric parameters, four dealt
    -- questions across two papers, and a fifth question that was never dealt.
    -- A second channel with its own paper proves the discard stays inside the
    -- channel it belongs to.
    -- -----------------------------------------------------------------------
    INSERT INTO IRF (IRFId, ClientId, StartDate, EndDate, RisebirdHr, Discount,
                     IsBiometricEnabled, PriorityLevel, IsPaused,
                     IsAIInterview, AIInterviewType)
    VALUES (@IRFCode, @ClientId, GETDATE(), GETDATE(), @RisebirdHr, 0,
            0, 3, 0, 1, 2);
    SET @IRFId = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO IRF (IRFId, ClientId, StartDate, EndDate, RisebirdHr, Discount,
                     IsBiometricEnabled, PriorityLevel, IsPaused,
                     IsAIInterview, AIInterviewType)
    VALUES (@OtherCode, @ClientId, GETDATE(), GETDATE(), @RisebirdHr, 0,
            0, 3, 0, 1, 2);
    SET @OtherIRFId = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO TransTechSkill (IRFId, SkillName) VALUES (@IRFId, 'System Design');
    INSERT INTO TransTechSkill (IRFId, SkillName) VALUES (@IRFId, 'Concurrency');

    INSERT INTO SkillChannelSampleQuestions (IRFId, Comment, SkillName, CreatedBy)
    VALUES (@IRFId, N'Design a rate limiter.', N'System Design', 'rbtest');
    SET @Q1 = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO SkillChannelSampleQuestions (IRFId, Comment, SkillName, CreatedBy)
    VALUES (@IRFId, N'Design a URL shortener.', N'System Design', 'rbtest');
    SET @Q2 = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO SkillChannelSampleQuestions (IRFId, Comment, SkillName, CreatedBy)
    VALUES (@IRFId, N'Explain a data race.', N'Concurrency', 'rbtest');
    SET @Q3 = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO SkillChannelSampleQuestions (IRFId, Comment, SkillName, CreatedBy)
    VALUES (@IRFId, N'Explain optimistic locking.', N'Concurrency', 'rbtest');
    SET @Q4 = CAST(SCOPE_IDENTITY() AS int);

    -- Never dealt into a paper.
    INSERT INTO SkillChannelSampleQuestions (IRFId, Comment, SkillName, CreatedBy)
    VALUES (@IRFId, N'Spare question, not in any set.', N'System Design', 'rbtest');
    SET @Loose = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO SkillChannelSampleQuestions (IRFId, Comment, SkillName, CreatedBy)
    VALUES (@OtherIRFId, N'A question on the other channel.', N'System Design', 'rbtest');
    SET @OtherQ = CAST(SCOPE_IDENTITY() AS int);

    INSERT INTO AIQuestionSets (IRFId, SetName, TotalMinutes, TotalPoints, PromptId, PromptVersion, CreatedBy, CreatedOn)
    VALUES (@IRFId, 'A', 20, 10, 'pmpt_rbtest', '1', 'rbtest', GETDATE());
    SET @SetA = CAST(SCOPE_IDENTITY() AS int);
    INSERT INTO AIQuestionSetItems (SetId, QuestionId, SkillName) VALUES (@SetA, @Q1, N'System Design');
    INSERT INTO AIQuestionSetItems (SetId, QuestionId, SkillName) VALUES (@SetA, @Q3, N'Concurrency');

    INSERT INTO AIQuestionSets (IRFId, SetName, TotalMinutes, TotalPoints, PromptId, PromptVersion, CreatedBy, CreatedOn)
    VALUES (@IRFId, 'B', 20, 10, 'pmpt_rbtest', '1', 'rbtest', GETDATE());
    SET @SetB = CAST(SCOPE_IDENTITY() AS int);
    INSERT INTO AIQuestionSetItems (SetId, QuestionId, SkillName) VALUES (@SetB, @Q2, N'System Design');
    INSERT INTO AIQuestionSetItems (SetId, QuestionId, SkillName) VALUES (@SetB, @Q4, N'Concurrency');

    INSERT INTO AIQuestionSets (IRFId, SetName, TotalMinutes, TotalPoints, PromptId, PromptVersion, CreatedBy, CreatedOn)
    VALUES (@OtherIRFId, 'A', 20, 10, 'pmpt_rbtest', '1', 'rbtest', GETDATE());
    SET @OtherSet = CAST(SCOPE_IDENTITY() AS int);
    INSERT INTO AIQuestionSetItems (SetId, QuestionId, SkillName) VALUES (@OtherSet, @OtherQ, N'System Design');

    INSERT INTO InterviewMeetings (IRFId, SessionId) VALUES (@IRFCode, NEWID());
    SET @InterviewId = CAST(SCOPE_IDENTITY() AS int);

    -- =======================================================================
    -- Case 1 -- a question that was never dealt into a paper. Nothing about
    -- sets should come into it, or editing the pool of a channel that uses
    -- sets becomes impossible.
    -- =======================================================================
    DELETE FROM @Outcome;
    INSERT INTO @Outcome (Success, [Message], SetsDiscarded)
    EXEC dbo.usp_DeleteSkillSampleQuestions @questionid = @Loose;

    SELECT @Success = Success, @Message = [Message], @Discarded = SetsDiscarded FROM @Outcome;
    SET @MainSets = (SELECT COUNT(*) FROM AIQuestionSets WHERE IRFId = @IRFId);
    SET @QuestionsLeft = (SELECT COUNT(*) FROM SkillChannelSampleQuestions WHERE id = @Loose);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Question never dealt into a set',
           'removed, no set touched',
           CASE WHEN @Success = 1 AND @Discarded = 0 AND @QuestionsLeft = 0 AND @MainSets = 2
                THEN 'PASS' ELSE 'FAIL' END,
           N'success=' + ISNULL(CAST(@Success AS varchar(1)), N'(null)')
           + N'; discarded=' + ISNULL(CAST(@Discarded AS varchar(10)), N'(null)')
           + N'; sets on channel=' + CAST(@MainSets AS varchar(10))
           + N'; ' + ISNULL(@Message, N'(no message)');

    -- =======================================================================
    -- Case 2 -- a dealt question on a channel whose papers were never used.
    -- The delete is allowed, and the channel's whole family of papers goes,
    -- because a set that lost a question no longer covers the rubric.
    -- =======================================================================
    DELETE FROM @Outcome;
    INSERT INTO @Outcome (Success, [Message], SetsDiscarded)
    EXEC dbo.usp_DeleteSkillSampleQuestions @questionid = @Q1;

    SELECT @Success = Success, @Message = [Message], @Discarded = SetsDiscarded FROM @Outcome;
    SET @MainSets = (SELECT COUNT(*) FROM AIQuestionSets WHERE IRFId = @IRFId);
    SET @Items = (SELECT COUNT(*) FROM AIQuestionSetItems i
                  JOIN AIQuestionSets s ON s.id = i.SetId WHERE s.IRFId = @IRFId);
    SET @QuestionsLeft = (SELECT COUNT(*) FROM SkillChannelSampleQuestions WHERE id = @Q1);
    SET @OtherSets = (SELECT COUNT(*) FROM AIQuestionSets WHERE IRFId = @OtherIRFId);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Dealt question, no paper in use',
           'removed, channel sets discarded, other channel untouched',
           CASE WHEN @Success = 1 AND @Discarded = 2 AND @QuestionsLeft = 0
                     AND @MainSets = 0 AND @Items = 0 AND @OtherSets = 1
                THEN 'PASS' ELSE 'FAIL' END,
           N'success=' + ISNULL(CAST(@Success AS varchar(1)), N'(null)')
           + N'; discarded=' + ISNULL(CAST(@Discarded AS varchar(10)), N'(null)')
           + N'; sets left on channel=' + CAST(@MainSets AS varchar(10))
           + N'; items left=' + CAST(@Items AS varchar(10))
           + N'; other channel sets=' + CAST(@OtherSets AS varchar(10))
           + N'; ' + ISNULL(@Message, N'(no message)');

    -- =======================================================================
    -- Case 3 -- the surviving questions are still there. Case 2 discards
    -- papers, not the pool; a delete that took the other questions with it
    -- would pass case 2 and be a disaster.
    -- =======================================================================
    SET @QuestionsLeft = (SELECT COUNT(*) FROM SkillChannelSampleQuestions
                          WHERE id IN (@Q2, @Q3, @Q4));

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Pool after a discard',
           'the other three questions survive',
           CASE WHEN @QuestionsLeft = 3 THEN 'PASS' ELSE 'FAIL' END,
           N'questions left=' + CAST(@QuestionsLeft AS varchar(10));

    -- =======================================================================
    -- Case 4 -- re-deal the papers and bind one to an interview, then try to
    -- remove a question from the BOUND paper. This is the case that used to
    -- shorten a live interview.
    -- =======================================================================

    -- Cleared explicitly rather than relying on case 2 having discarded them.
    -- A case that depends on an earlier case's outcome lets one broken
    -- behaviour hide every case after it.
    DELETE FROM InterviewAIQuestionSet
    WHERE SetId IN (SELECT id FROM AIQuestionSets WHERE IRFId = @IRFId);
    DELETE FROM AIQuestionSetItems
    WHERE SetId IN (SELECT id FROM AIQuestionSets WHERE IRFId = @IRFId);
    DELETE FROM AIQuestionSets WHERE IRFId = @IRFId;

    INSERT INTO AIQuestionSets (IRFId, SetName, TotalMinutes, TotalPoints, PromptId, PromptVersion, CreatedBy, CreatedOn)
    VALUES (@IRFId, 'A', 20, 10, 'pmpt_rbtest', '1', 'rbtest', GETDATE());
    SET @SetA = CAST(SCOPE_IDENTITY() AS int);
    INSERT INTO AIQuestionSetItems (SetId, QuestionId, SkillName) VALUES (@SetA, @Q2, N'System Design');
    INSERT INTO AIQuestionSetItems (SetId, QuestionId, SkillName) VALUES (@SetA, @Q3, N'Concurrency');

    INSERT INTO AIQuestionSets (IRFId, SetName, TotalMinutes, TotalPoints, PromptId, PromptVersion, CreatedBy, CreatedOn)
    VALUES (@IRFId, 'B', 20, 10, 'pmpt_rbtest', '1', 'rbtest', GETDATE());
    SET @SetB = CAST(SCOPE_IDENTITY() AS int);
    INSERT INTO AIQuestionSetItems (SetId, QuestionId, SkillName) VALUES (@SetB, @Q4, N'Concurrency');

    INSERT INTO InterviewAIQuestionSet (InterviewId, SetId, AssignedOn)
    VALUES (@InterviewId, @SetA, GETDATE());

    DELETE FROM @Outcome;
    INSERT INTO @Outcome (Success, [Message], SetsDiscarded)
    EXEC dbo.usp_DeleteSkillSampleQuestions @questionid = @Q2;

    SELECT @Success = Success, @Message = [Message], @Discarded = SetsDiscarded FROM @Outcome;
    SET @QuestionsLeft = (SELECT COUNT(*) FROM SkillChannelSampleQuestions WHERE id = @Q2);
    SET @MainSets = (SELECT COUNT(*) FROM AIQuestionSets WHERE IRFId = @IRFId);
    SET @Bindings = (SELECT COUNT(*) FROM InterviewAIQuestionSet WHERE InterviewId = @InterviewId);
    SET @Items = (SELECT COUNT(*) FROM AIQuestionSetItems WHERE SetId = @SetA);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Question in a paper an interview is locked to',
           'refused, nothing changed',
           CASE WHEN @Success = 0 AND @QuestionsLeft = 1 AND @MainSets = 2
                     AND @Bindings = 1 AND @Items = 2
                     AND @Message LIKE '%already locked to their sets%'
                THEN 'PASS' ELSE 'FAIL' END,
           N'success=' + ISNULL(CAST(@Success AS varchar(1)), N'(null)')
           + N'; question still there=' + CAST(@QuestionsLeft AS varchar(10))
           + N'; sets=' + CAST(@MainSets AS varchar(10))
           + N'; bindings=' + CAST(@Bindings AS varchar(10))
           + N'; items in bound set=' + CAST(@Items AS varchar(10))
           + N'; ' + ISNULL(@Message, N'(no message)');

    -- =======================================================================
    -- Case 5 -- the refusal is scoped to the channel, not to one paper. Q4
    -- sits in the UNBOUND paper B, but discarding B's family would have to
    -- take the bound paper A with it. Refusing per-set instead of per-channel
    -- would either destroy a live paper or trip the foreign key.
    -- =======================================================================
    DELETE FROM @Outcome;
    INSERT INTO @Outcome (Success, [Message], SetsDiscarded)
    EXEC dbo.usp_DeleteSkillSampleQuestions @questionid = @Q4;

    SELECT @Success = Success, @Message = [Message], @Discarded = SetsDiscarded FROM @Outcome;
    SET @QuestionsLeft = (SELECT COUNT(*) FROM SkillChannelSampleQuestions WHERE id = @Q4);
    SET @MainSets = (SELECT COUNT(*) FROM AIQuestionSets WHERE IRFId = @IRFId);
    SET @Bindings = (SELECT COUNT(*) FROM InterviewAIQuestionSet WHERE InterviewId = @InterviewId);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Question in an unbound paper on a channel that has one in use',
           'refused, the bound paper survives',
           CASE WHEN @Success = 0 AND @QuestionsLeft = 1 AND @MainSets = 2 AND @Bindings = 1
                THEN 'PASS' ELSE 'FAIL' END,
           N'success=' + ISNULL(CAST(@Success AS varchar(1)), N'(null)')
           + N'; question still there=' + CAST(@QuestionsLeft AS varchar(10))
           + N'; sets=' + CAST(@MainSets AS varchar(10))
           + N'; bindings=' + CAST(@Bindings AS varchar(10))
           + N'; ' + ISNULL(@Message, N'(no message)');

    -- =======================================================================
    -- Case 6 -- positive control. Once the binding is gone the same delete
    -- must succeed. Without this, a procedure that refused everything would
    -- score a pass on cases 4 and 5.
    -- =======================================================================
    DELETE FROM InterviewAIQuestionSet WHERE InterviewId = @InterviewId;

    DELETE FROM @Outcome;
    INSERT INTO @Outcome (Success, [Message], SetsDiscarded)
    EXEC dbo.usp_DeleteSkillSampleQuestions @questionid = @Q4;

    SELECT @Success = Success, @Message = [Message], @Discarded = SetsDiscarded FROM @Outcome;
    SET @QuestionsLeft = (SELECT COUNT(*) FROM SkillChannelSampleQuestions WHERE id = @Q4);
    SET @MainSets = (SELECT COUNT(*) FROM AIQuestionSets WHERE IRFId = @IRFId);

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Control -- same delete once no paper is in use',
           'removed, sets discarded',
           CASE WHEN @Success = 1 AND @QuestionsLeft = 0 AND @Discarded = 2 AND @MainSets = 0
                THEN 'PASS' ELSE 'FAIL' END,
           N'success=' + ISNULL(CAST(@Success AS varchar(1)), N'(null)')
           + N'; discarded=' + ISNULL(CAST(@Discarded AS varchar(10)), N'(null)')
           + N'; sets left=' + CAST(@MainSets AS varchar(10))
           + N'; ' + ISNULL(@Message, N'(no message)');

    -- =======================================================================
    -- Case 7 -- no dangling set items anywhere. This is the condition that
    -- made the original bug invisible: the runtime joins through this table,
    -- so an item pointing at a deleted question just disappears.
    -- =======================================================================
    SET @Items = (SELECT COUNT(*)
                  FROM AIQuestionSetItems i
                  WHERE NOT EXISTS (SELECT 1 FROM SkillChannelSampleQuestions q WHERE q.id = i.QuestionId));

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Set items pointing at a deleted question',
           'none anywhere in the database',
           CASE WHEN @Items = 0 THEN 'PASS' ELSE 'FAIL' END,
           N'dangling items=' + CAST(@Items AS varchar(10));

    -- =======================================================================
    -- Case 8 -- a question id that does not exist is reported, not silently
    -- treated as a success.
    -- =======================================================================
    DELETE FROM @Outcome;
    INSERT INTO @Outcome (Success, [Message], SetsDiscarded)
    EXEC dbo.usp_DeleteSkillSampleQuestions @questionid = -424242;

    SELECT @Success = Success, @Message = [Message] FROM @Outcome;

    INSERT INTO @Results (TestCase, Expectation, Outcome, Detail)
    SELECT 'Question id that does not exist',
           'reported as not removed',
           CASE WHEN @Success = 0 AND @Message LIKE '%no longer exists%'
                THEN 'PASS' ELSE 'FAIL' END,
           N'success=' + ISNULL(CAST(@Success AS varchar(1)), N'(null)')
           + N'; ' + ISNULL(@Message, N'(no message)');

    ROLLBACK TRANSACTION RBQDelTests;

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
       CASE WHEN NOT EXISTS (SELECT 1 FROM IRF WHERE IRFId IN (@IRFCode, @OtherCode))
                 AND NOT EXISTS (SELECT 1 FROM InterviewMeetings WHERE IRFId IN (@IRFCode, @OtherCode))
            THEN 'PASS' ELSE 'FAIL' END,
       N'open transactions left behind=' + CAST(@@TRANCOUNT AS varchar(10));

SELECT Seq, TestCase, Expectation, Outcome, Detail FROM @Results ORDER BY Seq;

SET @Failures = (SELECT COUNT(*) FROM @Results WHERE Outcome = 'FAIL');

IF @Failures = 0
    SELECT 'ALL PASS -- a question behind a paper in use cannot be removed, a question behind an unused paper takes that channel''s papers with it, and no set item is ever left pointing at a deleted question.' AS Verdict;
ELSE
    SELECT CAST(@Failures AS varchar(10)) + ' FAILED -- see the rows above. Do not ship the guard until these pass.' AS Verdict;
