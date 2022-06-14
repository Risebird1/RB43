IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'IRFRecommendations')
BEGIN 

CREATE TABLE [dbo].[IRFRecommendations](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[CloneIRFId] [int] NOT NULL,
	[CategoryId] [int] NULL,
	[CompetencyLevelId] [int] NULL,
	[SeniorityLevelId] [varchar](50) NULL,
	[CountryType] [int] NULL,
	[Status] [bit] NULL,
	[CreatedOn] [datetime] NULL,
	[ModifiedOn] [datetime] NULL,
	[AssessmentGroupId] [int] NULL,
	[CompanyType] [int] NULL,
	[DifficultyLevel] [varchar](50) NULL,--saved as ProfileType in Assessment Group but the display Name even there is Difficulty Level
	[EmploymentType] [varchar](20) NULL,
	[PerHourRate] [int] NULL,
	CONSTRAINT [FK_IRFRecommendations_CategoryId] FOREIGN KEY([CategoryId]) REFERENCES [dbo].[AssessmentGroupCategory] ([id]),
	CONSTRAINT [FK_IRFRecommendations_CloneIRFId] FOREIGN KEY([CloneIRFId]) REFERENCES [dbo].[ClonableSkillChannel] ([id]),
	constraint [FK_IRFRecommendations_AssessmentGroupId] FOREIGN KEY([AssessmentGroupId]) REFERENCES [dbo].[AssessmentGroup] ([id]),
 CONSTRAINT [PK_IRFRecommendations] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END

ELSE
BEGIN

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CompanyType')
      AND Object_ID = Object_ID(N'IRFRecommendations'))
BEGIN
    ALTER TABLE IRFRecommendations
    Add CompanyType INT NULL
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SeniorityLevelId')
      AND Object_ID = Object_ID(N'IRFRecommendations'))
BEGIN
    ALTER TABLE IRFRecommendations
    Alter column SeniorityLevelId [varchar](50) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'DifficultyLevel')
      AND Object_ID = Object_ID(N'IRFRecommendations'))
BEGIN
    ALTER TABLE IRFRecommendations
    Add DifficultyLevel [varchar](50) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'EmploymentType')
      AND Object_ID = Object_ID(N'IRFRecommendations'))
BEGIN
    ALTER TABLE IRFRecommendations
    Add EmploymentType [varchar](20) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PerHourRate')
      AND Object_ID = Object_ID(N'IRFRecommendations'))
BEGIN
    ALTER TABLE IRFRecommendations
    Add PerHourRate INT NULL
END

END

Go



