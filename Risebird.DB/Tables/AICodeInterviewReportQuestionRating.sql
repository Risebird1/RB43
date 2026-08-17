-- Per question rows of an AI code interview report copy (see AICodeInterviewReport).
-- TranscriptText holds the text shown beside the recorded answer: the speech to text output on
-- the AI copy, the expert's corrected version on the expert copy.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'AICodeInterviewReportQuestionRating')
BEGIN
CREATE TABLE [dbo].[AICodeInterviewReportQuestionRating](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[ReportId] [int] NOT NULL,
	[QuestionId] [int] NULL,
	[BotInterviewQuestionVideoId] [int] NULL,
	[DisplayOrder] [int] NOT NULL,
	[QuestionText] [nvarchar](max) NULL,
	[Rating] [decimal](5,2) NULL,
	[Verdict] [nvarchar](200) NULL,
	[Comment] [nvarchar](max) NULL,
	[CommunicationRating] [decimal](5,2) NULL,
	[CommunicationComment] [nvarchar](max) NULL,
	[RubricRatingsJson] [nvarchar](max) NULL,
	[TranscriptText] [nvarchar](max) NULL,
 CONSTRAINT [PK_AICodeInterviewReportQuestionRating] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
ELSE
BEGIN
	IF NOT EXISTS(
		SELECT * FROM sys.columns
		WHERE Name IN (N'TranscriptText') AND Object_ID = Object_ID(N'AICodeInterviewReportQuestionRating'))
	BEGIN
		ALTER TABLE AICodeInterviewReportQuestionRating Add TranscriptText nvarchar(max) NULL
	END
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys WHERE name = 'FK_AICodeIntQuestionRating_Report'
)
BEGIN
    ALTER TABLE [dbo].[AICodeInterviewReportQuestionRating]
    WITH CHECK ADD CONSTRAINT [FK_AICodeIntQuestionRating_Report] FOREIGN KEY([ReportId])
    REFERENCES [dbo].[AICodeInterviewReport] ([id])
    ON DELETE CASCADE;
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'UQ_AICodeIntQuestionRating_ReportOrder'
    AND object_id = OBJECT_ID('AICodeInterviewReportQuestionRating')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_AICodeIntQuestionRating_ReportOrder
    ON AICodeInterviewReportQuestionRating (ReportId, DisplayOrder);
END
GO
