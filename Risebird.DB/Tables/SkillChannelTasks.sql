/****** Object:  Table [dbo].[SkillChannelTasks]    Script Date: 5/11/2024 06:19:35 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SkillChannelTasks')
BEGIN 


CREATE TABLE [dbo].[SkillChannelTasks](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NULL,
	[SkillChannel] [varchar](15) NULL,
	[TaskName] [varchar](50) NULL,
	[TaskValue] [varchar](50) NULL,
	[Interviewer] [varchar](150) NULL,
	[Status] [int] NULL,
	[CreatedOn] [datetime] NULL,
	[PerformedBy] [varchar](150) NULL,
	[Comments] [varchar](500) NULL,
 CONSTRAINT [PK_SkillChannelTasks] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]


END
GO




