IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ScheduleInterviewEvent')
BEGIN 
CREATE TABLE [dbo].[ScheduleInterviewEvent](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[EventName] [varchar](150) NULL,
	[SkillChannel] [varchar](50) NULL,
	[StartDate] [datetime] NULL,
	[EndDate] [datetime] NULL,
	[duration] [int] NULL,
	[Uniqueid] [varchar](50) NULL,
	[IsActive] [bit] NULL,
	[IsSingleUse] [bit] NULL,
	[IsOnetimeUsed] [bit] NULL,
	[SkillChannelRelation] [varchar](50) NULL,
	[Concurrency] [int] NULL,
	[BufferTime] [int] default 12,
	[CandidateEmail] [varchar](200) NULL,
 CONSTRAINT [PK_ScheduleInterviewEvent] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsSingleUse')
      AND Object_ID = Object_ID(N'ScheduleInterviewEvent'))
	BEGIN
		ALTER TABLE [ScheduleInterviewEvent]
		Add [IsSingleUse] bit
	END
	IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsOnetimeUsed')
      AND Object_ID = Object_ID(N'ScheduleInterviewEvent'))
	BEGIN
		ALTER TABLE [ScheduleInterviewEvent]
		Add [IsOnetimeUsed] bit
	END
	IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SkillChannelRelation')
      AND Object_ID = Object_ID(N'ScheduleInterviewEvent'))
	BEGIN
		ALTER TABLE [ScheduleInterviewEvent]
		Add [SkillChannelRelation] varchar(50)
	END
	IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Concurrency')
      AND Object_ID = Object_ID(N'ScheduleInterviewEvent'))
	BEGIN
		ALTER TABLE [ScheduleInterviewEvent]
		Add Concurrency int null
	END
	IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'BufferTime')
      AND Object_ID = Object_ID(N'ScheduleInterviewEvent'))
	BEGIN
		ALTER TABLE [ScheduleInterviewEvent]
		Add BufferTime int default 12
	END
	IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CandidateEmail')
      AND Object_ID = Object_ID(N'ScheduleInterviewEvent'))
	BEGIN
		ALTER TABLE [ScheduleInterviewEvent]
		Add CandidateEmail varchar(200)
	END
GO


