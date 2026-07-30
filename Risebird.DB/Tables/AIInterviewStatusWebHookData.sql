
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'AIInterviewStatusWebHookData')
BEGIN
CREATE TABLE [dbo].[AIInterviewStatusWebHookData](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [varchar](25) NOT NULL,
	[CandidateEmailId] [varchar](200) NOT NULL,
	[Status][varchar](50),
	[PerformedAt][datetime],
 CONSTRAINT [PK_AIInterviewStatusWebHookData] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End

GO
