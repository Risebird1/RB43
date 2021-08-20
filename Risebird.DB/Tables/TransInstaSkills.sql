/****** Object:  Table [dbo].[TransInstaSkills]    Script Date: 18-08-2021 22:47:50 ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInstaSkills')
BEGIN 


CREATE TABLE [dbo].[TransInstaSkills](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NULL,
	[SkillId] [Varchar](50) NULL,
	[LevelId] [int] NULL,
	[ActualLevelId] [int] NULL,
 CONSTRAINT [PK_TransInstaSkills] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


