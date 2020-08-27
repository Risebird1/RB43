IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewShareDetails')
BEGIN  
CREATE TABLE [dbo].[InterviewShareDetails](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Interviewid] [int] NULL,
	[Emailid] [varchar](100) NULL,
	[SharedOn] [datetime] NULL,
	[SharedBy] [int] NULL,
 CONSTRAINT [PK_InterviewShareDetails] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


