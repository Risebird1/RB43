/****** Object:  Table [dbo].[ResumeEvaluator]    Script Date: 09-12-2019 7.34.59 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ResumeEvaluator')
BEGIN 
CREATE TABLE [dbo].[ResumeEvaluator](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
    FOREIGN KEY([UserId]) REFERENCES [dbo].[UserList] ([id]),

 CONSTRAINT [PK_ResumeEvaluator] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
End
GO



