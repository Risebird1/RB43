
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CandidateStatusWebHookData')
BEGIN
CREATE TABLE [dbo].[CandidateStatusWebHookData](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [varchar](25) NOT NULL,
	[CandidateEmailId] [varchar](200) NOT NULL,
	[Status][varchar](50),
	[PerformedAt][datetime],
 CONSTRAINT [PK_CandidateStatusWebHookData] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End

GO




