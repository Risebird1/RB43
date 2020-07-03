IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CandidateCuratedScore')
BEGIN
CREATE TABLE [dbo].[CandidateCuratedScore](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NULL,
	[CandidateEmailId] [varchar](200) NULL,
	[CuratedEmailId] [varchar](200) NULL,
	[Score] [int] NULL,	
 CONSTRAINT [PK_CandidateCuratedScore] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
End
GO