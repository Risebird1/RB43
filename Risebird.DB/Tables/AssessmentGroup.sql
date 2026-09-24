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
    [AssessmentVerificationType][varchar](5) NULL,
    [CodeLanguage][varchar](100) NULL,
    [MCQSetsValue][int] NULL,
    [AvailabilityMailSentOn][datetime] NULL,
    [IsMasterAG][bit] NULL,
    [USTJobTitle][varchar](200) NULL
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
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AssessmentVerificationType')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    Add AssessmentVerificationType varchar(5) NULL 
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CodeLanguage')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    Add CodeLanguage varchar(100) NULL 
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'MCQSetsValue')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    Add MCQSetsValue int NULL 
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AvailabilityMailSentOn')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    Add AvailabilityMailSentOn datetime NULL 
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsMasterAG')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    Add IsMasterAG bit NULL 
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'USTJobTitle')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    Add USTJobTitle varchar(200) NULL 
END

GO



-- ---------------------------------------------------------------------------------------------
-- Catch-all Assessment Group for AI interview channels.
--
-- An AI Audio Adaptive channel has no human interviewer panel, so which Assessment Group it
-- belongs to is irrelevant to how it runs. The column still has to be populated: roughly 19
-- stored procedures INNER JOIN AssessmentGroup on IRF.AssessmentGroupId, and an unmatched value
-- makes the channel vanish from those surfaces (AI resume screening, dashboard ribbon counts,
-- the channel edit screen) with no error.
--
-- So AI screening channels are stamped with this one reusable row rather than a new row per job
-- description. Two properties keep it out of everything else:
--   Isinstaavailable = 0 / IsRelevant = 0  -- stays out of the relevant/insta AG lists
--   no InterviewerProfile rows bound to it -- human-panel routing can never select it
--
-- Idempotent: safe to re-run.
-- ---------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.AssessmentGroup WHERE AssessmentGroup = 'AIInterview')
BEGIN
    INSERT INTO dbo.AssessmentGroup
        ([AssessmentGroup], [PrimarySkills], [Isinstaavailable], [IsRelevant], [IsMasterAG])
    VALUES
        ('AIInterview', 'AI Interview', 0, 0, 0)
END
GO
