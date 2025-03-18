IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'DebugLogger')
BEGIN 

CREATE TABLE [dbo].[DebugLogger](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Type] varchar(50) not null,
	[Messsage] [varchar](max) NULL,
	[PerformedBy] [varchar](100) NOT NULL,
	[PerformedAt] [datetime] NOT NULL,
	
 CONSTRAINT [PK_DebugLogger] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

End

