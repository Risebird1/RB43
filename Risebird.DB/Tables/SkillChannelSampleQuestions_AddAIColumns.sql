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
