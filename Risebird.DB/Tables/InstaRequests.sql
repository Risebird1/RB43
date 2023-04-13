/****** Object:  Table [dbo].[InstaRequests]    Script Date: 18-08-2021 22:48:38 ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InstaRequests')
BEGIN 

CREATE TABLE [dbo].[InstaRequests](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InstaRequestName] [varchar](50) NULL,
	[AssessmentGroup] [int] NULL,
	[JDLink] [varchar](1000) NULL,
	[RequestedBy] [varchar](150) NULL,
	[CreatedOn] [datetime] NULL,
	[CategoryId] [int] NULL,
	[CountryType] [int] NULL,
	[CompanyTypes] [int] NULL,
	[JobLevel] [int] NULL,
	[DifficultyLevel] [int] NULL,
	[EmploymentType] [int] NULL
 CONSTRAINT [PK_InstaRequests] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
ELSE
BEGIN
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CategoryId')
      AND Object_ID = Object_ID(N'InstaRequests'))
BEGIN
    ALTER TABLE InstaRequests
    Add CategoryId INT
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CountryType')
      AND Object_ID = Object_ID(N'InstaRequests'))
BEGIN
    ALTER TABLE InstaRequests
    Add CountryType INT
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CompanyTypes')
      AND Object_ID = Object_ID(N'InstaRequests'))
BEGIN
    ALTER TABLE InstaRequests
    Add CompanyTypes INT
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'JobLevel')
      AND Object_ID = Object_ID(N'InstaRequests'))
BEGIN
    ALTER TABLE InstaRequests
    Add JobLevel INT
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'DifficultyLevel')
      AND Object_ID = Object_ID(N'InstaRequests'))
BEGIN
    ALTER TABLE InstaRequests
    Add DifficultyLevel INT
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'EmploymentType')
      AND Object_ID = Object_ID(N'InstaRequests'))
BEGIN
    ALTER TABLE InstaRequests
    Add EmploymentType INT
END

End
GO


