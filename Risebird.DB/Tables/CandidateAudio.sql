/****** Object:  Table [dbo].[CandidateAudio]    Script Date: 20-01-2020 7.03.48 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[CandidateAudio](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[CandidateId] [int] NOT NULL,
	[UploadedByUserId] [int] NOT NULL,
	[PhraseDetail] [nvarchar](max) NULL,
	[IsVoiceBiometric] [bit] NOT NULL,
	[AudioFileLink] [nvarchar](max) NULL,
	FOREIGN KEY([CandidateId]) REFERENCES [dbo].[CandidatesMaster] ([id]),
	FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),
	FOREIGN KEY([UploadedByUserId]) REFERENCES [dbo].[UserList] ([id]),
 CONSTRAINT [PK_CandidateAudio] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO



