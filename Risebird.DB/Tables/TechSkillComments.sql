/****** Object:  Table [dbo].[TechSkillComments]    Script Date: 5/28/2021 6:40:07 AM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TechSkillComments')

Begin
	CREATE TABLE [dbo].[TechSkillComments](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[Skill] [int] NULL,
	[Comment] [varchar](max) NULL,
	[UserId][int] NULL,
	[SkillName][varchar](500) NULL,
	 CONSTRAINT [PK_TechSkillComments] PRIMARY KEY CLUSTERED 
	(
		[id] ASC
	)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

	ALTER TABLE [dbo].[TechSkillComments]  WITH CHECK ADD FOREIGN KEY([IRFId])
	REFERENCES [dbo].[IRF] ([id])
	

	--ALTER TABLE [dbo].[TechSkillComments]  WITH CHECK ADD FOREIGN KEY([Skill])
	--REFERENCES [dbo].[Skill] ([id])
End

Else
Begin

	IF NOT EXISTS(
		SELECT *
		FROM sys.columns
		WHERE Name IN (N'UserId')
		  AND Object_ID = Object_ID(N'TechSkillComments'))
	BEGIN
		Alter table TechSkillComments
		Add UserId int
	END
	IF NOT EXISTS(
		SELECT *
		FROM sys.columns
		WHERE Name IN (N'SkillName')
		  AND Object_ID = Object_ID(N'TechSkillComments'))
	BEGIN
		Alter table TechSkillComments
		Add SkillName varchar(250)
	END
	IF EXISTS(
		SELECT *
		FROM sys.columns
		WHERE Name IN (N'SkillName')
		  AND Object_ID = Object_ID(N'TechSkillComments'))
	BEGIN
		ALTER TABLE TechSkillComments
		ALTER COLUMN SkillName VARCHAR(500) NULL;
	END
End

GO




 