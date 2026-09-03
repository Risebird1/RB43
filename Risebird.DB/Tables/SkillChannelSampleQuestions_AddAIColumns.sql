IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('SkillChannelSampleQuestions') AND Name = 'GeneratedPayload'
)
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    ADD GeneratedPayload nvarchar(max) NULL
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('SkillChannelSampleQuestions') AND Name = 'IsGeneratedByAi'
)
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    ADD IsGeneratedByAi bit NOT NULL CONSTRAINT DF_SCSQ_IsGeneratedByAi DEFAULT 0
END
GO

-- The MCQ assessment section, placed in the question order like any other question type.
--
-- Following the same pattern as IsTypingSpeedAssessment / IsListeningComprehension /
-- IsCodingQuestion: one flag on one row, and the delivery order decides where in the interview
-- the section appears. That lets a channel author put the written test where they want it -
-- typically after a warm-up question or two - rather than it being bolted to the start or end.
--
-- The row's Comment is what the avatar says to introduce the section; the questions themselves
-- come from screen.risebird.io and are never stored here.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('SkillChannelSampleQuestions') AND Name = 'IsMcqAssessmentSection'
)
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    ADD IsMcqAssessmentSection bit NULL CONSTRAINT DF_SCSQ_IsMcqAssessmentSection DEFAULT 0
END
GO

-- The Synthesia render currently in flight for this question.
--
-- Rendering routinely takes longer than a web request can wait, so the generate action polls for
-- a while and then gives up. Before this column that give-up was terminal: the next click created
-- a brand new render and gave up again, so a question whose video simply took a few minutes could
-- never get one, and every attempt left an orphaned render behind on the Synthesia account.
--
-- Holding the id lets a retry resume the render already in progress instead of starting another.
-- Cleared once the finished video has been downloaded and AIQuestionVideoLink is set.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('SkillChannelSampleQuestions') AND Name = 'AIQuestionVideoJobId'
)
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    ADD AIQuestionVideoJobId varchar(100) NULL
END
GO
