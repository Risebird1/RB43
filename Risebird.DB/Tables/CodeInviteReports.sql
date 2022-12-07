IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CodeInviteReports')
BEGIN

CREATE TABLE [dbo].[CodeInviteReports](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[inviteId] [varchar](60) NULL,
	[submissionId] [varchar](60) NULL,
	[questionid] [int] NULL,
	[questionName] [varchar](max) NULL,
	[questionDescription] [varchar](max) NULL,
	[candidateSolution] [varchar](max) NULL,
	[achievedScore] [int] NULL,
	[questionScore] [int] NULL,
	[questionstatus] [varchar](20) NULL,
	[input] [varchar](100) NULL,
	[expectedOutput] [varchar](100) NULL,
	[actualOutput] [varchar](100) NULL,
	[testcasestatus] [varchar](20) NULL,
 CONSTRAINT [PK_CodeInviteReports] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO


