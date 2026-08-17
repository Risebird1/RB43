-- Field by field record of what the expert changed relative to the AI report. Recomputed on every
-- submit and used as the source of truth by the read only audit view.
-- Severity: 0 = none/informational, 1 = Minor, 2 = Major, 3 = Critical.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'AICodeInterviewReportDiscrepancy')
BEGIN
CREATE TABLE [dbo].[AICodeInterviewReportDiscrepancy](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[SessionId] [varchar](50) NOT NULL,
	[AssignmentId] [int] NULL,
	[ExpertReportId] [int] NULL,
	[FieldType] [varchar](50) NOT NULL,
	[QuestionId] [int] NULL,
	[DisplayOrder] [int] NULL,
	[FieldKey] [nvarchar](300) NULL,
	[AIValue] [nvarchar](max) NULL,
	[ExpertValue] [nvarchar](max) NULL,
	[IsDiscrepancy] [bit] NOT NULL CONSTRAINT [DF_AICodeIntDiscrepancy_IsDiscrepancy] DEFAULT (0),
	[Severity] [tinyint] NOT NULL CONSTRAINT [DF_AICodeIntDiscrepancy_Severity] DEFAULT (0),
	[CreatedAt] [datetime] NULL,
 CONSTRAINT [PK_AICodeInterviewReportDiscrepancy] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'IX_AICodeIntDiscrepancy_Session'
    AND object_id = OBJECT_ID('AICodeInterviewReportDiscrepancy')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_AICodeIntDiscrepancy_Session
    ON AICodeInterviewReportDiscrepancy (SessionId);
END
GO
