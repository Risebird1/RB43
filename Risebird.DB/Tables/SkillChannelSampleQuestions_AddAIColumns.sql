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
