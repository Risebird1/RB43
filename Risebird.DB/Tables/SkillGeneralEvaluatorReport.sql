/****** Object:  Table [dbo].[SkillGeneralEvaluatorReport]    Script Date: 06-01-2020 7.04.38 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SkillGeneralEvaluatorReport](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[RequiredLevelId] [int] NOT NULL,
	[Type] [nvarchar](200) NOT NULL,
	[ActualLevelId] [int] NOT NULL,
	[WeightageRequired] [int] NOT NULL,
	[WeightageActual] [int] NOT NULL,
	[Value] [varchar](500) NULL,
	[CandidateId] [int] NULL,
	[IRFId] [int] NULL,
	FOREIGN KEY([ActualLevelId]) REFERENCES [dbo].[Level] ([id]),
	FOREIGN KEY([CandidateId]) REFERENCES [dbo].[CandidatesMaster] ([id]),
	FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),
	FOREIGN KEY([RequiredLevelId]) REFERENCES [dbo].[Level] ([id]),
 CONSTRAINT [PK_SkillGeneralEvaluatorReport] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[SkillGeneralEvaluatorReport]  WITH CHECK ADD FOREIGN KEY([ActualLevelId])
REFERENCES [dbo].[Level] ([id])
GO

ALTER TABLE [dbo].[SkillGeneralEvaluatorReport]  WITH CHECK ADD FOREIGN KEY([CandidateId])
REFERENCES [dbo].[CandidatesMaster] ([id])
GO

ALTER TABLE [dbo].[SkillGeneralEvaluatorReport]  WITH CHECK ADD FOREIGN KEY([IRFId])
REFERENCES [dbo].[IRF] ([id])
GO

ALTER TABLE [dbo].[SkillGeneralEvaluatorReport]  WITH CHECK ADD FOREIGN KEY([RequiredLevelId])
REFERENCES [dbo].[Level] ([id])
GO


