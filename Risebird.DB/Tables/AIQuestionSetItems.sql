-- Identity order IS ask order. usp_SaveAIQuestionSets inserts ORDER BY SortOrder (a TVP-only
-- column), so reading back ORDER BY id yields the intended order. Do not add a sort column
-- to this table and do not re-sort the launch payload.
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'AIQuestionSetItems')
BEGIN
CREATE TABLE [dbo].[AIQuestionSetItems](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[SetId] [int] NOT NULL,
	[QuestionId] [int] NOT NULL,
	[SkillName] [nvarchar](500) NULL,
 CONSTRAINT [PK_AIQuestionSetItems] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = N'FK_AIQuestionSetItems_AIQuestionSets')
BEGIN
    ALTER TABLE [dbo].[AIQuestionSetItems] WITH CHECK
    ADD CONSTRAINT [FK_AIQuestionSetItems_AIQuestionSets] FOREIGN KEY([SetId])
    REFERENCES [dbo].[AIQuestionSets] ([id])
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_AIQuestionSetItems_SetId'
    AND object_id = OBJECT_ID(N'dbo.AIQuestionSetItems'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AIQuestionSetItems_SetId]
    ON [dbo].[AIQuestionSetItems] ([SetId] ASC)
END
GO
