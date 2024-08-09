/****** Object:  Table [dbo].[Project]    Script Date: 20-09-2019 11.42.49 AM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Project')
BEGIN 

CREATE TABLE [dbo].[Project](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](100) NULL,
	[IsDeleted] [bit] NOT NULL DEFAULT '0',
	[Location] [varchar](100) NULL,
	[ClientId] [int] NOT NULL DEFAULT '0',
	[EndClient] [varchar](100) NULL,
	[ProjectOwnerId] [varchar](100) NULL,
	[CreatedOn] [date] NULL,
    [ProjectUniqueid][varchar](20),
    [SkillSetsevaluate][int],
 CONSTRAINT [PK_Project] PRIMARY KEY CLUSTERED 
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
    WHERE Name IN (N'CreatedOn')
      AND Object_ID = Object_ID(N'Project'))
BEGIN
    ALTER TABLE Project
    Add CreatedOn Date
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ClientId')
      AND Object_ID = Object_ID(N'Project'))
BEGIN
    ALTER TABLE Project
    Add ClientId INT NOT NULL DEFAULT 0
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SkillSetsevaluate')
      AND Object_ID = Object_ID(N'Project'))
BEGIN
    ALTER TABLE Project
    Add SkillSetsevaluate int
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ProjectUniqueid')
      AND Object_ID = Object_ID(N'Project'))
BEGIN
    ALTER TABLE Project
    Add ProjectUniqueid varchar(20)
END

END


Go




