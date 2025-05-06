/****** Object:  Table [dbo].[InterviewerDirectApplication]    ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewerDirectApplication')
BEGIN
CREATE TABLE [dbo].[InterviewerDirectApplication](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[EmailId] [varchar](100) NOT NULL,
	[Phone] [varchar](20) NOT NULL,
	[ResumeLink] [varchar](max) NOT NULL,
	[CreatedAt][datetime] Null,
	[ModifiedAt][datetime] NULL,
	[CreatedBy][varchar](100) NOT NULL,
	[ModifiedBy][varchar](100) NULL,
	[OpenAIResponse] [varchar](max) NULL,
	[AIVerdict] [varchar](50)  NULL,
	[AIEvaluationPerformedBy] [varchar](100) NULL,
	[AIEvaluationGeneratedAt] datetime NULL,
	[CompanyName] [varchar](100) NULL,
	[FirstName] [varchar](150) NULL,
	[LastName] [varchar](150) NULL,
	[isVerified] [bit] NULL,
	[IsEmployed] [bit] NULL,
	[DOB] datetime NULL,
	[EVGeneratedBy] [varchar](100) NULL,
	[EVGeneratedAt] datetime NULL,
	[ReferredBy] [varchar](200) NULL,
	[AIAnalysisScore] [int] NULL,
 CONSTRAINT [PK_InterviewerDirectApplication] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] --TEXTIMAGE_ON [PRIMARY]
END

IF not EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'OpenAIResponse')
    AND Object_ID = Object_ID(N'InterviewerDirectApplication'))
BEGIN
	ALTER TABLE InterviewerDirectApplication
	add OpenAIResponse varchar(max) NULL
END

IF not EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'AIVerdict')
    AND Object_ID = Object_ID(N'InterviewerDirectApplication'))
BEGIN
	ALTER TABLE InterviewerDirectApplication
	add AIVerdict varchar(50) NULL
END

IF not EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'AIEvaluationGeneratedAt')
    AND Object_ID = Object_ID(N'InterviewerDirectApplication'))
BEGIN
	ALTER TABLE InterviewerDirectApplication
	add AIEvaluationGeneratedAt datetime NULL
END
IF not EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'AIEvaluationPerformedBy')
    AND Object_ID = Object_ID(N'InterviewerDirectApplication'))
BEGIN
	ALTER TABLE InterviewerDirectApplication
	add AIEvaluationPerformedBy varchar(100) NULL
END

IF not EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'CompanyName')
    AND Object_ID = Object_ID(N'InterviewerDirectApplication'))
BEGIN
	ALTER TABLE InterviewerDirectApplication
	add CompanyName varchar(100) NULL
END

IF not EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'isVerified')
    AND Object_ID = Object_ID(N'InterviewerDirectApplication'))
BEGIN
	ALTER TABLE InterviewerDirectApplication
	add isVerified bit NULL
END

IF not EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'IsEmployed')
    AND Object_ID = Object_ID(N'InterviewerDirectApplication'))
BEGIN
	ALTER TABLE InterviewerDirectApplication
	add IsEmployed bit NULL
END

IF not EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'FirstName')
    AND Object_ID = Object_ID(N'InterviewerDirectApplication'))
BEGIN
	ALTER TABLE InterviewerDirectApplication
	add FirstName varchar(150) NULL
END

IF not EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'LastName')
    AND Object_ID = Object_ID(N'InterviewerDirectApplication'))
BEGIN
	ALTER TABLE InterviewerDirectApplication
	add LastName varchar(150) NULL
END

IF not EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'DOB')
    AND Object_ID = Object_ID(N'InterviewerDirectApplication'))
BEGIN
	ALTER TABLE InterviewerDirectApplication
	add DOB datetime NULL
END

IF not EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'EVGeneratedAt')
    AND Object_ID = Object_ID(N'InterviewerDirectApplication'))
BEGIN
	ALTER TABLE InterviewerDirectApplication
	add EVGeneratedAt datetime NULL
END

IF not EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'EVGeneratedBy')
    AND Object_ID = Object_ID(N'InterviewerDirectApplication'))
BEGIN
	ALTER TABLE InterviewerDirectApplication
	add EVGeneratedBy varchar(100) NULL
END

IF not EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'ReferredBy')
    AND Object_ID = Object_ID(N'InterviewerDirectApplication'))
BEGIN
	ALTER TABLE InterviewerDirectApplication
	add ReferredBy varchar(200) NULL
END

IF not EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'AIAnalysisScore')
    AND Object_ID = Object_ID(N'InterviewerDirectApplication'))
BEGIN
	ALTER TABLE InterviewerDirectApplication
	add AIAnalysisScore int NULL
END

GO



