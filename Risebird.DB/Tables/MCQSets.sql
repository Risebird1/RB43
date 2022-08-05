
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'MCQSets')
BEGIN 

CREATE TABLE [dbo].[MCQSets](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[PartitionKey] [varchar](50) NULL,
	[Title] [varchar](50) NULL,
	[CompentencyLevel] [int] NULL,
	[PrimarySkills] [varchar](100) NULL,
	[ProfileType] [varchar](50) NULL,
	[SeniorityLevel] [varchar](50) NULL,
	[EmploymentType] [varchar](20) NULL,
	[SecondarySkill] [varchar](100) NULL,
	[Timer] [varchar](5) NULL,
	[QuestionCount] [int] NULL,
	[CreatedOn] [datetime] NULL,
	[CompanyType] [int] NULL,
	[CountryType] [int] NULL,
	[PerHourRate] [int] NULL,
	[Clients] [varchar](max) NULL,
	[AadhaarVerification] [bit] NULL
	
 CONSTRAINT [PK_MCQSets] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
ELSE

BEGIN

IF EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'ProfileType')
    AND Object_ID = Object_ID(N'MCQSets'))
BEGIN
	ALTER TABLE MCQSets
	Alter column ProfileType [varchar](50) NULL
END

IF NOT EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'ProfileType')
    AND Object_ID = Object_ID(N'MCQSets'))
BEGIN
	ALTER TABLE MCQSets
	Add ProfileType [varchar](50) NULL
END

IF NOT EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'SeniorityLevel')
    AND Object_ID = Object_ID(N'MCQSets'))
BEGIN
	ALTER TABLE MCQSets
	Add SeniorityLevel [varchar](50) NULL
END

IF NOT EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'EmploymentType')
    AND Object_ID = Object_ID(N'MCQSets'))
BEGIN
	ALTER TABLE MCQSets
	Add EmploymentType [varchar](20) NULL
END

IF NOT EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'CompanyType')
    AND Object_ID = Object_ID(N'MCQSets'))
BEGIN
	ALTER TABLE MCQSets
	Add CompanyType int NULL
END

IF NOT EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'CountryType')
    AND Object_ID = Object_ID(N'MCQSets'))
BEGIN
	ALTER TABLE MCQSets
	Add CountryType int NULL
END

IF NOT EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'PerHourRate')
    AND Object_ID = Object_ID(N'MCQSets'))
BEGIN
	ALTER TABLE MCQSets
	Add PerHourRate int NULL
END

IF NOT EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'Clients')
    AND Object_ID = Object_ID(N'MCQSets'))
BEGIN
	ALTER TABLE MCQSets
	Add Clients [varchar](max) NULL
END

IF NOT EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'AadhaarVerification')
    AND Object_ID = Object_ID(N'MCQSets'))
BEGIN
	ALTER TABLE MCQSets
	Add AadhaarVerification bit
END

END

GO
