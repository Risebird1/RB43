IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'AICodeInterviewQuestionSetItem')
BEGIN
CREATE TABLE [dbo].[AICodeInterviewQuestionSetItem](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[QuestionSetId] [int] NOT NULL,
	[QuestionBankId] [int] NOT NULL,
	[DisplayOrder] [int] NOT NULL CONSTRAINT [DF_AIQSI_Order] DEFAULT (0),
 CONSTRAINT [PK_AICodeInterviewQuestionSetItem] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'IX_AIQSI_Set'
      AND object_id = OBJECT_ID('AICodeInterviewQuestionSetItem')
)
BEGIN
    CREATE INDEX IX_AIQSI_Set ON dbo.AICodeInterviewQuestionSetItem (QuestionSetId, DisplayOrder)
END
GO
