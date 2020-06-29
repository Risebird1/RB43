IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerSkill')
BEGIN

CREATE TABLE [dbo].[TransInterviewerSkill](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerProfileId] [int] NOT NULL,
	[SkillId] [int] NULL,
	[SkillName] [varchar](150) NULL,
	[Category] [varchar](150) NULL,
	[Ip] [varchar](150) NULL,
	[Level] [varchar](50) NULL,
	[SkillExperience] [nvarchar](50) NULL,
	[Nature] [varchar](50) NULL,
	
	CONSTRAINT [FK__IProfileID] FOREIGN KEY (InterviewerProfileId) REFERENCES InterviewerProfile(id),
	CONSTRAINT [FK__SkillID] FOREIGN KEY (SkillId) REFERENCES Skill(id),
 CONSTRAINT [PK_TransInterviewerSkill] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) 
)

END

ELSE
BEGIN

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Category')
      AND Object_ID = Object_ID(N'TransInterviewerSkill'))
BEGIN
    ALTER TABLE TransInterviewerSkill
    Add Category varchar(150) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Ip')
      AND Object_ID = Object_ID(N'TransInterviewerSkill'))
BEGIN
    ALTER TABLE TransInterviewerSkill
    Add Ip varchar (150) NULL,
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SkillExperience')
      AND Object_ID = Object_ID(N'TransInterviewerSkill'))
BEGIN
    ALTER TABLE TransInterviewerSkill
    Add SkillExperience nvarchar (50) NULL,
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Nature')
      AND Object_ID = Object_ID(N'TransInterviewerSkill'))
BEGIN
    ALTER TABLE TransInterviewerSkill
    Add Nature varchar (50) NULL
END


End