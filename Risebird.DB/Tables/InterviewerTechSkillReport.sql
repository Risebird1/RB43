IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewerTechSkillReport')
BEGIN 

CREATE TABLE [dbo].[InterviewerTechSkillReport](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NOT NULL,
	[RequiredLevelId] [int] NOT NULL,
	[SkillId] [int] NOT NULL,
	[ActualLevelId] [int] NOT NULL,
	[WeightageRequired] [float] NULL,
	[WeightageActual] [float] NULL,
 CONSTRAINT [PK_InterviewerTechSkillReport] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

ALTER TABLE [dbo].[InterviewerTechSkillReport]  WITH CHECK ADD FOREIGN KEY([ActualLevelId])
REFERENCES [dbo].[LevelTech] ([id])
GO

ALTER TABLE [dbo].[InterviewerTechSkillReport]  WITH CHECK ADD FOREIGN KEY([InterviewId])
REFERENCES [dbo].[UnapprovedInterviewerMeetings] ([id])
GO

ALTER TABLE [dbo].[InterviewerTechSkillReport]  WITH CHECK ADD FOREIGN KEY([RequiredLevelId])
REFERENCES [dbo].[LevelTech] ([id])
GO

ALTER TABLE [dbo].[InterviewerTechSkillReport]  WITH CHECK ADD FOREIGN KEY([SkillId])
REFERENCES [dbo].[Skill] ([id])
GO


