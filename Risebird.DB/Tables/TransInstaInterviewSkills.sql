IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInstaInterviewSkills')
BEGIN 

CREATE TABLE [dbo].[TransInstaInterviewSkills](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NULL,
	[SkillId] [varchar](50) NULL,
	[LevelId] [int] NULL,
	[ActualLevelId] [int] NULL,
 CONSTRAINT [PK_TransInstaInterviewSkills] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


