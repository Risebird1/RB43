/****** Object:  Table [dbo].[ChatGroupUsers]    Script Date: 6/12/2020 4:37:54 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ChatGroupUsers')
BEGIN  

CREATE TABLE [dbo].[ChatGroupUsers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Groupid] [int] NULL,
	[Userid] [int] NULL,
 CONSTRAINT [PK_ChatGroupUsers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
End
GO


