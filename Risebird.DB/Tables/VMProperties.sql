/****** Object:  Table [dbo].[VMProperties]    Script Date: 16-09-2020 6.36.46 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'VMProperties')
BEGIN 

CREATE TABLE [dbo].[VMProperties](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](150) NOT NULL,
	[ResourceGroupName] [varchar](150) NOT NULL,
	[IpAddress] [varchar](100) NULL,
	[Status] [int] NULL,
	[IsAdded] [bit] NOT NULL,
	[IsRemoved] [bit] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
 CONSTRAINT [PK_VMProperties] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ENd
GO


