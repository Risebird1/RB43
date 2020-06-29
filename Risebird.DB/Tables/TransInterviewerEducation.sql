IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerEducation')
BEGIN

CREATE TABLE [dbo].[TransInterviewerEducation](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerProfileId] [int] NOT NULL,
	[EducationType] [varchar](100) NULL,
	[Degree] [nvarchar](100) NULL,
	[EducationMode] [varchar](150) NULL,
	[CollegeType] [nvarchar](100) NULL,
	[UniversityName] [nvarchar](200) NULL,
	[PassOutYear] [int] NULL,
	[AnyGap] [bit] NULL,
	
	CONSTRAINT [FK__IProfileID_Education] FOREIGN KEY (InterviewerProfileId) REFERENCES InterviewerProfile(id),
 CONSTRAINT [PK_TransEducation] PRIMARY KEY CLUSTERED 
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
    WHERE Name IN (N'Graduation')
      AND Object_ID = Object_ID(N'TransInterviewerEducation'))
BEGIN
    ALTER TABLE TransInterviewerEducation
    DROP COLUMN  Graduation
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Specialization')
      AND Object_ID = Object_ID(N'TransInterviewerEducation'))
BEGIN
    ALTER TABLE TransInterviewerEducation
    DROP COLUMN  Specialization
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Score')
      AND Object_ID = Object_ID(N'TransInterviewerEducation'))
BEGIN
    ALTER TABLE TransInterviewerEducation
    DROP COLUMN  Score
END



IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'EducationType')
      AND Object_ID = Object_ID(N'TransInterviewerEducation'))
BEGIN
    ALTER TABLE TransInterviewerEducation
    Add EducationType varchar (100) NULL
END


IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Degree')
      AND Object_ID = Object_ID(N'TransInterviewerEducation'))
BEGIN
    ALTER TABLE TransInterviewerEducation
    Add Degree nvarchar (100) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'EducationMode')
      AND Object_ID = Object_ID(N'TransInterviewerEducation'))
BEGIN
    ALTER TABLE TransInterviewerEducation
    Add EducationMode varchar (150) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'UniversityName')
      AND Object_ID = Object_ID(N'TransInterviewerEducation'))
BEGIN
    ALTER TABLE TransInterviewerEducation
    Add UniversityName nvarchar (200) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PassOutYear')
      AND Object_ID = Object_ID(N'TransInterviewerEducation'))
BEGIN
    ALTER TABLE TransInterviewerEducation
    Add PassOutYear int NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AnyGap')
      AND Object_ID = Object_ID(N'TransInterviewerEducation'))
BEGIN
    ALTER TABLE TransInterviewerEducation
    Add AnyGap bit NULL
END



End