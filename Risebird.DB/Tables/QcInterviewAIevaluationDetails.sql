IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'QcInterviewAIevaluationDetails')
BEGIN  

CREATE TABLE [dbo].[QcInterviewAIevaluationDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[RedmarkedQCId] [int] NULL,
	[Interviewid] [int] NULL,
	[FlagName] [varchar](200) NULL,
	[verdict] [varchar](20) NULL,
	[Remark] [varchar](500) NULL,
	[SourceType] [varchar](20) NULL,
 CONSTRAINT [PK_QcInterviewAIevaluationDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


