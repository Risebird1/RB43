IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewerProfile')
BEGIN
CREATE TABLE [dbo].[InterviewerProfile](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[NickName] [varchar](100) NOT NULL,
	[AltEmailID] [varchar](150) NULL,
	[DOB] [datetime] NULL,
	[Gender] [varchar](50) NULL,
	[AnnualSalary] [nvarchar](200) NULL,
	[Country] [varchar](200) NULL,
	[City] [varchar](200) NULL,
	[AltMobileNumber] [nvarchar](200) NULL,
	[CurrentRole] [varchar](150) NULL,
	[Experience] [decimal](10, 2) NULL,
	[PreferredDomain] [varchar](200) NULL,
	[GitHubLink] [varchar](200) NULL,
	[ResumeLink] [varchar](2000) NULL,
	[CodedResumeLink] [varchar](2000) NULL,
	[StackOverflowLink] [varchar](2000) NULL,
	[LinkedinLink] [varchar](2000) NULL,
	[Blog] [varchar](2000) NULL,
	[IsLeadInterviewer] [bit] NULL DEFAULT 0,
	[IsApproved] [bit] NOT NULL,
	[Isinstaavailable] [bit] NOT NULL,
	[Isavailable] [int] NULL DEFAULT 0,
	CONSTRAINT [UC_InterviewerProfile] UNIQUE (NickName),
	CONSTRAINT [FK_InterviewerProfile] FOREIGN KEY (UserId) REFERENCES UserList(id),
	CONSTRAINT [PK_InterviewerProfile] PRIMARY KEY CLUSTERED 
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
    WHERE Name IN (N'FirstName')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    DROP COLUMN  FirstName
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'LastName')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    DROP COLUMN  LastName
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'EmailId')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    DROP COLUMN  EmailId
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Location')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    DROP COLUMN  Location
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'MobileNumber')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    DROP COLUMN  MobileNumber
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsActive')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    DROP COLUMN  IsActive
END



IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'DOB')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add DOB datetime NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Gender')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add Gender varchar(50) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AnnualSalary')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add AnnualSalary nvarchar(200) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Country')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add Country varchar(200) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'City')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add City varchar(200) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'GitHubLink')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add GitHubLink nvarchar(max) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'StackOverflowLink')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add StackOverflowLink nvarchar(max) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'LinkedinLink')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add LinkedinLink nvarchar(max) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Blog')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add Blog nvarchar(max) NULL
END
IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AssessmentGroupId')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Alter Column AssessmentGroupId varchar(100) 
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AssessmentGroupId')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add AssessmentGroupId varchar(100) 
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Isinstaavailable')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add Isinstaavailable bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Isavailable')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add Isavailable int DEFAULT 0
END


End
