IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'AssessmentGroup')
BEGIN 

CREATE TABLE [dbo].[AssessmentGroup](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[AssessmentGroup] [varchar](100) NULL,
	[PrimarySkills] [varchar](max) NULL,
    [Isinstaavailable][bit] NULL,
    [Categoryid][int] NULL,
    [CompanyType][bit] NULL,
    [IsRelevant][int] NULL,
    [ProfileType][varchar](50) NULL,
    [CompetencyLevel][int] NULL,
    [Country][int] NULL,
    [PerHourRate][int] NULL,
    [SeniorityLevel][varchar](50) NULL,
    [EmploymentType][varchar](20) NULL,
 CONSTRAINT [PK_AssessmentGroup] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Isinstaavailable')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    Add Isinstaavailable bit NOT NULL DEFAULT 0
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Categoryid')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    Add Categoryid int NULL 
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CompanyType')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    Add CompanyType int NULL 
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsRelevant')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    Add IsRelevant bit NULL
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ProfileType')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    Add ProfileType varchar(50) NULL 
END
IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ProfileType')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    alter column ProfileType varchar(50) NULL 
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CompetencyLevel')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    Add CompetencyLevel int NULL 
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Country')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    Add Country int NULL 
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PerHourRate')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    Add PerHourRate int NULL 
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SeniorityLevel')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    Add SeniorityLevel varchar(50) NULL 
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'EmploymentType')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    Add EmploymentType varchar(20) NULL 
END
GO


