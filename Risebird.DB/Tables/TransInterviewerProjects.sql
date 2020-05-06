IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerProjects')
BEGIN

CREATE TABLE [dbo].[TransInterviewerProjects](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerProfileId] [int] NOT NULL,
	[TechnologyEnvironment] [nvarchar](200) NULL,
	[IsPrimarySkillUsed] [bit] NULL,
	[Description] [nvarchar](max) NULL,
	[IsCommercialProject] [bit] NULL,
	[NatureOfProject] [varchar](50) NULL,
	
	CONSTRAINT [FK__IProfileID_Projects] FOREIGN KEY (InterviewerProfileId) REFERENCES InterviewerProfile(id),
 CONSTRAINT [PK_TransInterviewerProjects] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) 
)

END

ELSE
BEGIN

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ProjectName')
      AND Object_ID = Object_ID(N'TransInterviewerProjects'))
BEGIN
    ALTER TABLE TransInterviewerProjects
    DROP COLUMN  ProjectName
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ClientName')
      AND Object_ID = Object_ID(N'TransInterviewerProjects'))
BEGIN
    ALTER TABLE TransInterviewerProjects
    DROP COLUMN  ClientName
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'StartDate')
      AND Object_ID = Object_ID(N'TransInterviewerProjects'))
BEGIN
    ALTER TABLE TransInterviewerProjects
    DROP COLUMN  StartDate
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'EndDate')
      AND Object_ID = Object_ID(N'TransInterviewerProjects'))
BEGIN
    ALTER TABLE TransInterviewerProjects
    DROP COLUMN  EndDate
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Role')
      AND Object_ID = Object_ID(N'TransInterviewerProjects'))
BEGIN
    ALTER TABLE TransInterviewerProjects
    DROP COLUMN  Role
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ProjectDescriptions')
      AND Object_ID = Object_ID(N'TransInterviewerProjects'))
BEGIN
    ALTER TABLE TransInterviewerProjects
    DROP COLUMN  ProjectDescriptions
END


IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'TechnologyEnvironment')
      AND Object_ID = Object_ID(N'TransInterviewerProjects'))
BEGIN
    ALTER TABLE TransInterviewerProjects
    Add TechnologyEnvironment nvarchar (200) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsPrimarySkillUsed')
      AND Object_ID = Object_ID(N'TransInterviewerProjects'))
BEGIN
    ALTER TABLE TransInterviewerProjects
    Add IsPrimarySkillUsed bit NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Description')
      AND Object_ID = Object_ID(N'TransInterviewerProjects'))
BEGIN
    ALTER TABLE TransInterviewerProjects
    Add Description nvarchar (max) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsCommercialProject')
      AND Object_ID = Object_ID(N'TransInterviewerProjects'))
BEGIN
    ALTER TABLE TransInterviewerProjects
    Add IsCommercialProject bit NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'NatureOfProject')
      AND Object_ID = Object_ID(N'TransInterviewerProjects'))
BEGIN
    ALTER TABLE TransInterviewerProjects
    Add NatureOfProject varchar (50) NULL
END

End
