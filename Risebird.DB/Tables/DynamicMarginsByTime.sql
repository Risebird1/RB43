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

GO

