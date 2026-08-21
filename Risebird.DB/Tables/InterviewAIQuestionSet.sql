-- One paper per interview, for the interview's whole life. The unique index on InterviewId is
-- what makes the launch idempotent: a second concurrent launch either re-reads the existing
-- row or loses the insert race and is then re-read by usp_AssignAIQuestionSetToInterview.
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'InterviewAIQuestionSet')
BEGIN
CREATE TABLE [dbo].[InterviewAIQuestionSet](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NOT NULL,
	[SetId] [int] NOT NULL,
	[AssignedOn] [datetime] NULL,
 CONSTRAINT [PK_InterviewAIQuestionSet] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'UX_InterviewAIQuestionSet_InterviewId'
    AND object_id = OBJECT_ID(N'dbo.InterviewAIQuestionSet'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_InterviewAIQuestionSet_InterviewId]
    ON [dbo].[InterviewAIQuestionSet] ([InterviewId] ASC)
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = N'FK_InterviewAIQuestionSet_AIQuestionSets')
BEGIN
    ALTER TABLE [dbo].[InterviewAIQuestionSet] WITH CHECK
    ADD CONSTRAINT [FK_InterviewAIQuestionSet_AIQuestionSets] FOREIGN KEY([SetId])
    REFERENCES [dbo].[AIQuestionSets] ([id])
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_InterviewAIQuestionSet_SetId'
    AND object_id = OBJECT_ID(N'dbo.InterviewAIQuestionSet'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_InterviewAIQuestionSet_SetId]
    ON [dbo].[InterviewAIQuestionSet] ([SetId] ASC)
END
GO
