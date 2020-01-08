/****** Object:  Table [dbo].[Recruiter]    Script Date: 08-01-2020 6.40.18 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Recruiter')
BEGIN

CREATE TABLE [dbo].[Recruiter](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[Domain] [varchar](100) NULL,
	FOREIGN KEY([UserId]) REFERENCES [dbo].[UserList] ([id]),
 CONSTRAINT [PK_Recruiter] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
GO




