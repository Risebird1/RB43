IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'AICodeInterviewQuestionReviewLog')
BEGIN
CREATE TABLE [dbo].[AICodeInterviewQuestionReviewLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[QuestionBankId] [int] NOT NULL,
	[FieldName] [varchar](80) NOT NULL,
	[AIValue] [nvarchar](max) NULL,
	[SMEValue] [nvarchar](max) NULL,
	[Severity] [tinyint] NOT NULL CONSTRAINT [DF_AIQRL_Severity] DEFAULT (1),
	[ChangedBy] [varchar](150) NULL,
	[ChangedOn] [datetime] NOT NULL CONSTRAINT [DF_AIQRL_ChangedOn] DEFAULT (GETUTCDATE()),
 CONSTRAINT [PK_AICodeInterviewQuestionReviewLog] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'IX_AIQRL_Question'
      AND object_id = OBJECT_ID('AICodeInterviewQuestionReviewLog')
)
BEGIN
    CREATE INDEX IX_AIQRL_Question ON dbo.AICodeInterviewQuestionReviewLog (QuestionBankId)
END
GO
