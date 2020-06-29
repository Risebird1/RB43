IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerProfessional')
BEGIN

CREATE TABLE [dbo].[TransInterviewerProfessional](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerProfileId] [int] NOT NULL,
	[CompanyName] [nvarchar](200) NULL,
	[Type] [varchar](50) NULL,
	[CompanyStage] [varchar](100) NULL,
	[Size] [nvarchar](50) NULL,
	[JobFunctionId] [int] NULL,
	[IndustryId] [int] NULL,
	[Headquarters] [varchar](100) NULL,
	[Role] [varchar](100) NULL,
	[Domain] [nvarchar](200) NULL,
	[SeniorityLevel] [varchar](50) NULL,
	[ExperienceId] [int] NULL,
	[WorkNature] [varchar](50) NULL,
	[PrimaryResponsibility] [varchar](50) NULL,
	[CompanyLinkedinPage] [nvarchar](max) NULL,
	[CompanyWebsite] [nvarchar](max) NULL,
	
	CONSTRAINT [FK__IProfileID_Professional] FOREIGN KEY (InterviewerProfileId) REFERENCES InterviewerProfile(id),
	CONSTRAINT [FK__ExperienceId] FOREIGN KEY (ExperienceId) REFERENCES ExperienceRanges(id),
	CONSTRAINT [FK__IndustryId] FOREIGN KEY (IndustryId) REFERENCES IndustryCodes(id),
	CONSTRAINT [FK__JobFunctionId] FOREIGN KEY (JobFunctionId) REFERENCES JobFunction(id),
 CONSTRAINT [PK_TransProfessional] PRIMARY KEY CLUSTERED 
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
    WHERE Name IN (N'CompanyType')
      AND Object_ID = Object_ID(N'TransInterviewerProfessional'))
BEGIN
    ALTER TABLE TransInterviewerProfessional
    DROP COLUMN  CompanyType
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'StartDate')
      AND Object_ID = Object_ID(N'TransInterviewerProfessional'))
BEGIN
    ALTER TABLE TransInterviewerProfessional
    DROP COLUMN  StartDate
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'EndDate')
      AND Object_ID = Object_ID(N'TransInterviewerProfessional'))
BEGIN
    ALTER TABLE TransInterviewerProfessional
    DROP COLUMN  EndDate
END



IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Type')
      AND Object_ID = Object_ID(N'TransInterviewerProfessional'))
BEGIN
    ALTER TABLE TransInterviewerProfessional
    Add Type varchar (50) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CompanyStage')
      AND Object_ID = Object_ID(N'TransInterviewerProfessional'))
BEGIN
    ALTER TABLE TransInterviewerProfessional
    Add CompanyStage varchar (100) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Size')
      AND Object_ID = Object_ID(N'TransInterviewerProfessional'))
BEGIN
    ALTER TABLE TransInterviewerProfessional
    Add Size nvarchar (50) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'JobFunctionId')
      AND Object_ID = Object_ID(N'TransInterviewerProfessional'))
BEGIN
    ALTER TABLE TransInterviewerProfessional
    Add JobFunctionId int NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IndustryId')
      AND Object_ID = Object_ID(N'TransInterviewerProfessional'))
BEGIN
    ALTER TABLE TransInterviewerProfessional
    Add IndustryId int NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Headquarters')
      AND Object_ID = Object_ID(N'TransInterviewerProfessional'))
BEGIN
    ALTER TABLE TransInterviewerProfessional
    Add Headquarters varchar (100) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Role')
      AND Object_ID = Object_ID(N'TransInterviewerProfessional'))
BEGIN
    ALTER TABLE TransInterviewerProfessional
    Add Role varchar (100) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SeniorityLevel')
      AND Object_ID = Object_ID(N'TransInterviewerProfessional'))
BEGIN
    ALTER TABLE TransInterviewerProfessional
    Add SeniorityLevel varchar (50) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ExperienceId')
      AND Object_ID = Object_ID(N'TransInterviewerProfessional'))
BEGIN
    ALTER TABLE TransInterviewerProfessional
    Add ExperienceId int NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'WorkNature')
      AND Object_ID = Object_ID(N'TransInterviewerProfessional'))
BEGIN
    ALTER TABLE TransInterviewerProfessional
    Add WorkNature varchar(50) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PrimaryResponsibility')
      AND Object_ID = Object_ID(N'TransInterviewerProfessional'))
BEGIN
    ALTER TABLE TransInterviewerProfessional
    Add PrimaryResponsibility varchar(50) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CompanyLinkedinPage')
      AND Object_ID = Object_ID(N'TransInterviewerProfessional'))
BEGIN
    ALTER TABLE TransInterviewerProfessional
    Add CompanyLinkedinPage nvarchar(max) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CompanyWebsite')
      AND Object_ID = Object_ID(N'TransInterviewerProfessional'))
BEGIN
    ALTER TABLE TransInterviewerProfessional
    Add CompanyWebsite nvarchar (max) NULL
END


End