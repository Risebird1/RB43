/****** Object:  Table [dbo].[TechSkillEvaluatorReport]    Script Date: 06-01-2020 7.04.24 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TechSkillEvaluatorReport](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[RequiredLevelId] [int] NOT NULL,
	[SkillId] [int] NOT NULL,
	[ActualLevelId] [int] NOT NULL,
	[WeightageRequired] [int] NOT NULL,
	[WeightageActual] [int] NOT NULL,
	[CandidateId] [int] NULL,
	[IRFId] [int] NULL,
	FOREIGN KEY([ActualLevelId]) REFERENCES [dbo].[LevelTech] ([id]),
	FOREIGN KEY([CandidateId]) REFERENCES [dbo].[CandidatesMaster] ([id]),
	FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),
	FOREIGN KEY([RequiredLevelId]) REFERENCES [dbo].[LevelTech] ([id]),
	FOREIGN KEY([SkillId]) REFERENCES [dbo].[Skill] ([id]),
 CONSTRAINT [PK_TechSkillEvaluatorReport] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO




