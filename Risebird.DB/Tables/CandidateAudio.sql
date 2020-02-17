/****** Object:  Table [dbo].[CandidateAudio]    Script Date: 20-01-2020 7.03.48 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CandidateAudio')
BEGIN
CREATE TABLE [dbo].[CandidateAudio](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[CandidateId] [int] NOT NULL,
	[UploadedByUserId] [int] NOT NULL,
	[PhraseDetail] [nvarchar](max) NULL,
	[IsVoiceBiometric] [bit] NOT NULL,
	[AudioFileLink] [nvarchar](max) NULL,
	[Description] [nvarchar](max) NULL,
	[IsEnrolled] [bit] NOT NULL,
	[EnrollmentMessage] [nvarchar](max) NULL,
	FOREIGN KEY([CandidateId]) REFERENCES [dbo].[CandidatesMaster] ([id]),
	FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),
	FOREIGN KEY([UploadedByUserId]) REFERENCES [dbo].[UserList] ([id]),
 CONSTRAINT [PK_CandidateAudio] PRIMARY KEY CLUSTERED 
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
    WHERE Name IN (N'Description')
      AND Object_ID = Object_ID(N'CandidateAudio'))
BEGIN
    ALTER TABLE CandidateAudio
    Add [Description] NVARCHAR(max)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsEnrolled')
      AND Object_ID = Object_ID(N'CandidateAudio'))
BEGIN
    ALTER TABLE CandidateAudio
    Add IsEnrolled BIT NOT NULL DEFAULT 0
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'EnrollmentMessage')
      AND Object_ID = Object_ID(N'CandidateAudio'))
BEGIN
    ALTER TABLE CandidateAudio
    Add EnrollmentMessage NVARCHAR(max)
END


END
GO



