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


--INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (1, N'New')
--INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (2, N'In Progress')
--INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (3, N'Waiting For Response')
--INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (4, N'Scheduled')
--INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (5, N'Irrelevant')
--INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (6, N'Duplicate')
--INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (7, N'Not Interested')
--INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (8, N'Got Another Offer')
--INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (9, N'Cold')
--INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (10, N'Hold')
--INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (11, N'Invitation Sent')
--INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (12, N'Added')
--INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (13, N'Updated')
--INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (14, N'Meeting Status')
--INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (15, N'Interview Status')
--INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (16, N'Called')
--INSERT [dbo].[CandidateStatusOptionsMst] ([id], [StatusValue]) VALUES (17, N'Not Responding')


update CandidateStatusOptionsMst set StatusValue='Reminder Call' where id=13

