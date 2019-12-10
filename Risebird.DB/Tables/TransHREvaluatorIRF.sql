/****** Object:  Table [dbo].[TransHREvaluatorIRF]    Script Date: 10-12-2019 7.04.36 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransHREvaluatorIRF')
BEGIN 

CREATE TABLE [dbo].[TransHREvaluatorIRF](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[CandidateId] [int] NOT NULL,
	[ResumeEvaluatorEmailId] [varchar](200) NULL,
	[HREmailId] [varchar](200) NULL,
	FOREIGN KEY([CandidateId]) REFERENCES [dbo].[CandidatesMaster] ([id]),
	FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),
 CONSTRAINT [PK_TransHREvaluatorIRF] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
End
GO




