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
 CONSTRAINT [PK_ScheduleInterviewEvent] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


