/****** Object:  Table [dbo].[Project]    Script Date: 20-09-2019 11.42.49 AM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Project')
BEGIN 

CREATE TABLE [dbo].[Project](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](100) NULL,
	[IsDeleted] [bit] NOT NULL DEFAULT '0',
	[Location] [varchar](100) NULL,
	[Client] [varchar](100) NULL,
	[EndClient] [varchar](100) NULL,
	[ProjectOwnerId] [varchar](100) NULL,
 CONSTRAINT [PK_Project] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
Go




