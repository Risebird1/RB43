/****** Object:  Table [dbo].[CandidateStatus]    Script Date: 24-11-2020 2.35.22 PM ******/

IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CandidateStatus')
BEGIN
CREATE TABLE [dbo].[CandidateStatus](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[CandidateId] [int] NOT NULL,
	[AudioFileLink] [nvarchar](max) NULL,
	[Comments] [varchar](max) NULL,
	[Date] [datetime] NULL,
	[Status] [int] NULL,
	[PerformedBy] [varchar](100) NULL
	FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),
	FOREIGN KEY([CandidateId]) REFERENCES [dbo].[CandidatesMaster] ([id]),
 CONSTRAINT [PK_CandidateStatus] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

End
Else

Begin
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PerformedBy')
      AND Object_ID = Object_ID(N'CandidateStatus'))
BEGIN
    ALTER TABLE CandidateStatus
    Add [PerformedBy] VARCHAR(100)
END

End


GO




