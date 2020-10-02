/****** Object:  Table [dbo].[InterviewReport]    Script Date: 02-10-2020 12.16.09 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ProctorVMQuestions')

BEGIN
CREATE TABLE [dbo].[ProctorVMQuestions](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[VmId] [int] NOT NULL,
	[Priority] [int] NOT NULL,
	[Question] [varchar](max) NOT NULL,
	[Detail] [varchar](max) NULL,
	[VideoLink] [varchar](500) NULL,
	[Time] [int] NULL,
	[Level] [int] NULL,
	[Skill] [varchar](500) NULL,
	FOREIGN KEY([VmId]) REFERENCES [dbo].[VMProperties] ([id]),

 CONSTRAINT [PK_ProctorVMQuestions] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]


END
Go










