IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CandidateVerification')
BEGIN
CREATE TABLE [dbo].[CandidateVerification](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[email] [varchar](200) NULL,
	[duplicate_account_detected] [bit] NULL,
	[first_name] [varchar](100) NULL,
	[last_name] [varchar](100) NULL,
	[middle_name] [varchar](50) NULL,
	[country] [varchar](100) NULL,
	[selected_type] [varchar](50) NULL,
	[face_match_confidence] [int] NULL,
	[reference] [varchar](100) NULL,
 CONSTRAINT [PK_CandidateVerification] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


