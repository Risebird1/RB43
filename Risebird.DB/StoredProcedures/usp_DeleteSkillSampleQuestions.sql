IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteSkillSampleQuestions'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteSkillSampleQuestions
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- This is the only place a sample question is ever deleted, which makes it the right place to stop
-- an AI interview question set losing a question underneath it.
--
-- usp_GetNextAIQuestionForInterview inner-joins this table, so a deleted row does not raise an
-- error: it simply vanishes from the set. The paper then serves fewer questions than the rubric has
-- parameters, and the candidate is scored on a parameter they were never asked about.
--
-- Two cases, deliberately handled differently:
--
--   Channel has a paper in use -> refuse. An interview is locked to its paper for life, so the
--                                 pool behind it has to stay as it is. The scope is the channel,
--                                 not this question's own set, because sets are re-dealt as a
--                                 family: usp_SaveAIQuestionSets refuses to re-create a channel
--                                 with any set in use, and this refuses on the same ground.
--
--   Dealt but never used       -> allow, and discard that channel's sets. A set that has lost a
--                                 question no longer covers the rubric, and a partial family is
--                                 worse than none. The Delivery Manager creates them again from
--                                 the new pool.
--
-- The discard can leave a channel switched on with no sets. That state is safe by design: the
-- runtime refuses to serve anything rather than falling back to the shared question order, and logs
-- an error saying so. Creating sets again clears it.
Create Procedure usp_DeleteSkillSampleQuestions
    @questionid int
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SetsDiscarded int = 0;
    DECLARE @InSet bit = 0;
    DECLARE @Bound bit = 0;
    DECLARE @Channels TABLE (IRFId int PRIMARY KEY);

    -- Guarded so this procedure still runs on a database where the question set tables have not
    -- been applied. Deferred name resolution keeps the branches below out of the plan unless they
    -- are actually reached.
    DECLARE @SetTablesExist bit =
        CASE WHEN OBJECT_ID('dbo.AIQuestionSets') IS NOT NULL
              AND OBJECT_ID('dbo.AIQuestionSetItems') IS NOT NULL
              AND OBJECT_ID('dbo.InterviewAIQuestionSet') IS NOT NULL
             THEN 1 ELSE 0 END;

    IF NOT EXISTS (SELECT 1 FROM SkillChannelSampleQuestions WHERE id = @questionid)
    BEGIN
        SELECT CAST(0 AS bit) AS Success,
               N'That question no longer exists.' AS [Message],
               0 AS SetsDiscarded;
        RETURN;
    END

    IF @SetTablesExist = 1
    BEGIN
        IF EXISTS (SELECT 1 FROM AIQuestionSetItems WHERE QuestionId = @questionid)
            SET @InSet = 1;

        INSERT INTO @Channels (IRFId)
        SELECT DISTINCT s.IRFId
        FROM AIQuestionSetItems i
        JOIN AIQuestionSets s ON s.id = i.SetId
        WHERE i.QuestionId = @questionid
          AND s.IRFId IS NOT NULL;

        IF EXISTS (SELECT 1
                   FROM AIQuestionSets s
                   JOIN InterviewAIQuestionSet ias ON ias.SetId = s.id
                   WHERE s.IRFId IN (SELECT IRFId FROM @Channels))
            SET @Bound = 1;
    END

    IF @Bound = 1
    BEGIN
        SELECT CAST(0 AS bit) AS Success,
               N'This question has been dealt into a question set, and one or more interviews on this channel are already locked to their sets. Removing it would leave an interview a question short and missing a rubric parameter.' AS [Message],
               0 AS SetsDiscarded;
        RETURN;
    END

    DECLARE @NestedTran bit = CASE WHEN @@TRANCOUNT > 0 THEN 1 ELSE 0 END;

    BEGIN TRY
        IF @NestedTran = 1 SAVE TRANSACTION DelSampleQ; ELSE BEGIN TRANSACTION;

        IF @InSet = 1
        BEGIN
            SELECT @SetsDiscarded = COUNT(*)
            FROM AIQuestionSets
            WHERE IRFId IN (SELECT IRFId FROM @Channels);

            DELETE FROM AIQuestionSetItems
            WHERE SetId IN (SELECT id FROM AIQuestionSets
                            WHERE IRFId IN (SELECT IRFId FROM @Channels));

            -- Belt and braces: a set row carrying no channel would otherwise keep an item behind.
            DELETE FROM AIQuestionSetItems WHERE QuestionId = @questionid;

            DELETE FROM AIQuestionSets
            WHERE IRFId IN (SELECT IRFId FROM @Channels);
        END

        DELETE FROM SkillChannelSampleQuestions WHERE id = @questionid;

        IF @NestedTran = 0 COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @NestedTran = 1
        BEGIN
            IF XACT_STATE() <> -1 ROLLBACK TRANSACTION DelSampleQ;
        END
        ELSE IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SELECT CAST(0 AS bit) AS Success,
               N'The question could not be removed. ' + ERROR_MESSAGE() AS [Message],
               0 AS SetsDiscarded;
        RETURN;
    END CATCH

    SELECT CAST(1 AS bit) AS Success,
           CASE WHEN @SetsDiscarded > 0
                THEN N'Question removed. The ' + CAST(@SetsDiscarded AS nvarchar(10))
                     + N' question set(s) on this channel were discarded because they no longer cover the rubric. Create sets again to re-deal the remaining questions.'
                ELSE N'Question removed.'
           END AS [Message],
           @SetsDiscarded AS SetsDiscarded;
END
GO
