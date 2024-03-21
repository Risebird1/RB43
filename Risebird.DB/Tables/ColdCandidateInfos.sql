IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ColdCandidateInfos')
BEGIN  
CREATE TABLE [dbo].[ColdCandidateInfos](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IsColdCandidate] [bit] NULL,
	[ColdMarkedBY] [varchar](100) NULL,
	[ColdMarkedDate] [datetime] NULL,
	[FKCandidateId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[ColdCandidateInfos]  WITH CHECK ADD FOREIGN KEY([FKCandidateId])
REFERENCES [dbo].[Candidate] ([id])
End
GO
