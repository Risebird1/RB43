/****** Object:  Table [dbo].[InterviewerDirectApplication]    ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewerDirectApplication')
BEGIN
CREATE TABLE [dbo].[InterviewerDirectApplication](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[EmailId] [varchar](100) NOT NULL,
	[Phone] [varchar](20) NOT NULL,
	[ResumeLink] [varchar](max) NOT NULL,
	[CreatedAt][datetime] Null,
	[ModifiedAt][datetime] NULL,
	[CreatedBy][varchar](100) NOT NULL,
	[ModifiedBy][varchar](100) NULL,
 CONSTRAINT [PK_InterviewerDirectApplication] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] --TEXTIMAGE_ON [PRIMARY]
END

GO



