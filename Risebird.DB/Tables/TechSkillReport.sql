IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TechSkillReport')
BEGIN
CREATE TABLE [dbo].[TechSkillReport](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NOT NULL,
	[RequiredLevelId] [int] NOT NULL,
	[SkillId] [int] NOT NULL,
	[ActualLevelId] [int] NOT NULL,
	[WeightageRequired] [float] NULL,
	[WeightageActual] [float] NULL,
	[TechComments] [varchar](100) NULL,
 CONSTRAINT [PK_TechSkillReport] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]


ALTER TABLE [dbo].[TechSkillReport]  WITH CHECK ADD FOREIGN KEY([ActualLevelId])
REFERENCES [dbo].[LevelTech] ([id])

ALTER TABLE [dbo].[TechSkillReport]  WITH CHECK ADD FOREIGN KEY([InterviewId])
REFERENCES [dbo].[InterviewMeetings] ([id])

ALTER TABLE [dbo].[TechSkillReport]  WITH CHECK ADD FOREIGN KEY([RequiredLevelId])
REFERENCES [dbo].[LevelTech] ([id])

ALTER TABLE [dbo].[TechSkillReport]  WITH CHECK ADD FOREIGN KEY([SkillId])
REFERENCES [dbo].[Skill] ([id])

END
ELSE
BEGIN
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'TechComments')
      AND Object_ID = Object_ID(N'TechSkillReport'))
BEGIN
    ALTER TABLE TechSkillReport
    Add [TechComments] varchar(100)
END
END
GO

