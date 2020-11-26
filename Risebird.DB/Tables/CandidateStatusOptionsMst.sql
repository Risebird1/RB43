/****** Object:  Table [dbo].[CandidateStatusOptionsMst]    Script Date: 24-11-2020 2.36.21 PM ******/

IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CandidateStatusOptionsMst')
BEGIN
CREATE TABLE [dbo].[CandidateStatusOptionsMst](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[StatusValue] [varchar](50) NULL,
 CONSTRAINT [PK_CandidateStatusOptionsMst] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End
GO


