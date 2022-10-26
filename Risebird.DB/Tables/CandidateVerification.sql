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
	[doc_face] [varchar](max) NULL,
	[passport_image] [varchar](max) NULL
 CONSTRAINT [PK_CandidateVerification] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
ELSE
BEGIN
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'doc_face')
      AND Object_ID = Object_ID(N'CandidateVerification'))
BEGIN
    ALTER TABLE CandidateVerification
    Add doc_face varchar(max)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'passport_image')
      AND Object_ID = Object_ID(N'CandidateVerification'))
BEGIN
    ALTER TABLE CandidateVerification
    Add passport_image varchar(max)
END
END
GO


