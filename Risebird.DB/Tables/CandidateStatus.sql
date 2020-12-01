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
	FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),
	FOREIGN KEY([CandidateId]) REFERENCES [dbo].[CandidatesMaster] ([id]),
 CONSTRAINT [PK_CandidateStatus] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

End
GO


INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (1, N'New')
INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (2, N'In Progress')
INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (3, N'Waiting For Response')
INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (4, N'Scheduled')
INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (5, N'Irrelevant')
INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (6, N'Duplicate')
INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (7, N'Not Interested')
INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (8, N'Got Another Offer')
INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (9, N'Cold')
INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (10, N'Hold')

