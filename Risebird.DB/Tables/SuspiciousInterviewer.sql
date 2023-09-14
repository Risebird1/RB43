IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SuspiciousInterviewer')
BEGIN
CREATE TABLE [dbo].[SuspiciousInterviewer](
	[Id] [int] IDENTITY(1,101) NOT NULL,
	[UserId] [int] NULL,
	[Comment] [varchar](100) NULL,
	[Createddate] [datetime] NULL,
	[CreatedBy] [varchar](50) NULL,
	[SuspiciousStatus] [bit] NULL,
	[ModifyBy] [varchar](100) NULL,
	[ModifyDate] [datetime] NULL,
 CONSTRAINT [PK_InterviewSuspicious] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END

GO


