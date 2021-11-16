/****** Object:  Table [dbo].[TransTechSkill]    Script Date: 11/15/2021 10:09:49 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransRecruiter')
BEGIN

CREATE TABLE [dbo].[TransTechSkill](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[SkillId] [int] NULL,
	[LevelId] [int] NULL,
	[Weightage] [int] NULL,
	[Concepts] [varchar](50) NULL,
	[Natureofquestion] [varchar](50) NULL,
	[Timelimit] [int] NULL,
 CONSTRAINT [PK_TransTechSkill] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] 
END
GO

ALTER TABLE [dbo].[TransTechSkill]  WITH CHECK ADD FOREIGN KEY([IRFId])
REFERENCES [dbo].[IRF] ([id])
GO

ALTER TABLE [dbo].[TransTechSkill]  WITH CHECK ADD FOREIGN KEY([LevelId])
REFERENCES [dbo].[LevelTech] ([id])
GO

ALTER TABLE [dbo].[TransTechSkill]  WITH CHECK ADD FOREIGN KEY([SkillId])
REFERENCES [dbo].[Skill] ([id])
GO
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Concepts')
      AND Object_ID = Object_ID(N'TransTechSkill'))
BEGIN
    ALTER TABLE TransTechSkill
    Add [Concepts] varchar(50)
END


IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Natureofquestion')
      AND Object_ID = Object_ID(N'TransTechSkill'))
BEGIN
    ALTER TABLE TransTechSkill
    Add [Natureofquestion] varchar(50)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Timelimit')
      AND Object_ID = Object_ID(N'TransTechSkill'))
BEGIN
    ALTER TABLE TransTechSkill
    Add [Timelimit] INT
END
