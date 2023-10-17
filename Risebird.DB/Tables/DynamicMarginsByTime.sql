/****** Object:  Table [dbo].[DynamicMarginsByTime]     ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'DynamicMarginsByTime')
BEGIN

CREATE TABLE [dbo].[DynamicMarginsByTime](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Day] [varchar](25) NOT NULL,
	[WeekDay] [int] NOT NULL,
	[Session1] [int] NOT NULL,
	[Session2][int] Not Null,
	[Session3][int] not Null,
	[Session4][int] not Null,
	
 CONSTRAINT [PK_DynamicMarginsByTime] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End
Go

SET IDENTITY_INSERT [dbo].[DynamicMarginsByTime] ON 

INSERT [dbo].[DynamicMarginsByTime] ([id], [Day], [WeekDay], [Session1], [Session2], [Session3], [Session4]) VALUES (1, N'Mon', 1, 20, 30, 40, 50)
INSERT [dbo].[DynamicMarginsByTime] ([id], [Day], [WeekDay], [Session1], [Session2], [Session3], [Session4]) VALUES (2, N'Tue', 2, 20, 30, 40, 50)
INSERT [dbo].[DynamicMarginsByTime] ([id], [Day], [WeekDay], [Session1], [Session2], [Session3], [Session4]) VALUES (3, N'Wed', 3, 20, 30, 40, 50)
INSERT [dbo].[DynamicMarginsByTime] ([id], [Day], [WeekDay], [Session1], [Session2], [Session3], [Session4]) VALUES (4, N'Thu', 4, 20, 30, 40, 50)
INSERT [dbo].[DynamicMarginsByTime] ([id], [Day], [WeekDay], [Session1], [Session2], [Session3], [Session4]) VALUES (5, N'Fri', 5, 20, 30, 40, 50)
INSERT [dbo].[DynamicMarginsByTime] ([id], [Day], [WeekDay], [Session1], [Session2], [Session3], [Session4]) VALUES (6, N'Sat', 6, 20, 30, 40, 50)
INSERT [dbo].[DynamicMarginsByTime] ([id], [Day], [WeekDay], [Session1], [Session2], [Session3], [Session4]) VALUES (7, N'Sun', 7, 20, 30, 40, 50)

SET IDENTITY_INSERT [dbo].[DynamicMarginsByTime] OFF

GO

