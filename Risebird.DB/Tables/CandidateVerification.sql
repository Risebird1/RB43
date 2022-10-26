IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CandidateVerification')
BEGIN  

CREATE TABLE [dbo].[CandidateVerification](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[email] [varchar](200) NULL,
	[name] [varchar](200) NULL,
	[birthDate] [varchar](25) NULL,
	[passportNumber] [varchar](15) NULL,
	[address] [varchar](max) NULL,
	[matchPercentage] [varchar](8) NULL,
	[nationality] [varchar](25) NULL,
	[gender] [varchar](2) NULL,
	[CreatedOn] [datetime] NULL,
 CONSTRAINT [PK_CandidateVerification] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO


