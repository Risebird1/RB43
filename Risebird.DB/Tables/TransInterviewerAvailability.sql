IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerAvailability')
BEGIN
CREATE TABLE [dbo].[TransInterviewerAvailability](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerProfileId] [int] NOT NULL,
	[Weekday] [varchar](50) NULL,
	[Weekend] [varchar](50) NULL,
	[IsAvailableOnPublicHolidays] [bit] NULL,
	
	CONSTRAINT [FK__IProfileID_Availability] FOREIGN KEY (InterviewerProfileId) REFERENCES InterviewerProfile(id),
 CONSTRAINT [PK_TransInterviewerAvailability] PRIMARY KEY CLUSTERED 
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
    WHERE Name IN (N'DayTimeSlot')
      AND Object_ID = Object_ID(N'TransInterviewerAvailability'))
BEGIN
    ALTER TABLE TransInterviewerAvailability
    DROP COLUMN  DayTimeSlot
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'DaySlot')
      AND Object_ID = Object_ID(N'TransInterviewerAvailability'))
BEGIN
    ALTER TABLE TransInterviewerAvailability
    DROP COLUMN  DaySlot
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'StartTime')
      AND Object_ID = Object_ID(N'TransInterviewerAvailability'))
BEGIN
    ALTER TABLE TransInterviewerAvailability
    DROP COLUMN  StartTime
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'EndTime')
      AND Object_ID = Object_ID(N'TransInterviewerAvailability'))
BEGIN
    ALTER TABLE TransInterviewerAvailability
    DROP COLUMN  EndTime
END



IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Weekday')
      AND Object_ID = Object_ID(N'TransInterviewerAvailability'))
BEGIN
    ALTER TABLE TransInterviewerAvailability
    Add Weekday nvarchar(50) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Weekend')
      AND Object_ID = Object_ID(N'TransInterviewerAvailability'))
BEGIN
    ALTER TABLE TransInterviewerAvailability
    Add Weekend nvarchar(50) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsAvailableOnPublicHolidays')
      AND Object_ID = Object_ID(N'TransInterviewerAvailability'))
BEGIN
    ALTER TABLE TransInterviewerAvailability
    Add IsAvailableOnPublicHolidays bit NULL
END



End