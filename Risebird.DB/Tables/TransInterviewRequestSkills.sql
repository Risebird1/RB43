IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewRequestSkills')
BEGIN 

CREATE TABLE [dbo].[TransInterviewRequestSkills](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NULL,
	[SkillId] [varchar](50) NULL,
	[LevelId] [int] NULL,
	[ActualLevelId] [int] NULL,
 CONSTRAINT [PK_TransInterviewRequestSkills] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


