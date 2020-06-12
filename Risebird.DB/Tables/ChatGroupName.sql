/****** Object:  Table [dbo].[ChatGroupName]    Script Date: 6/12/2020 4:36:59 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ChatGroupName')
BEGIN  

CREATE TABLE [dbo].[ChatGroupName](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[GroupName] [varchar](40) NULL,
 CONSTRAINT [PK_ChatGroupName] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


