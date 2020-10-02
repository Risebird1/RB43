IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CandidateJDScore')
BEGIN  
CREATE TABLE [dbo].[CandidateJDScore](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ClientJdid] [int] NULL,
	[IRFId] [int] NULL,
	[CandidateEmailId] [varchar](200) NULL,
	[CuratedEmailId] [varchar](200) NULL,
	[Score] [int] NULL,
 CONSTRAINT [PK_CandidateJDScore] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


