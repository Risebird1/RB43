/****** Object:  Table [dbo].[TransResumeEvaluator]    Script Date: 09-12-2019 7.36.13 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransHR')
BEGIN 

CREATE TABLE [dbo].[TransResumeEvaluator](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[ResumeEvaluatorId] [int] NOT NULL,
	CONSTRAINT [FK__TransResumeEvaluator] FOREIGN KEY (IRFId) REFERENCES IRF(id),
	CONSTRAINT [FK__TransResumeEvaluator_1] FOREIGN KEY (ResumeEvaluatorId) REFERENCES ResumeEvaluator(id),
 CONSTRAINT [PK_TransResumeEvaluator] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
End
GO



