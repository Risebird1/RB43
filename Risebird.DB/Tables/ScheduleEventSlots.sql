IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ScheduleEventSlots')
BEGIN 

CREATE TABLE [dbo].[ScheduleEventSlots](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Daytype] [varchar](15) NULL,
	[Start] [varchar](10) NULL,
	[End] [varchar](10) NULL,
	[dayCheck] [bit] NULL,
	[Eventid] [int] NULL,
 CONSTRAINT [PK_ScheduleEventSlots] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


