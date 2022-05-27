/****** Object:  Table [dbo].[ActivityTypeOptions]    Script Date: 13-01-2021 5.14.57 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ActivityTypeOptions')

BEGIN 
CREATE TABLE [dbo].[ActivityTypeOptions](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[ActivityName] [varchar](200) NOT NULL,
 CONSTRAINT [PK_ActivityTypeOptions] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
End

GO



SET IDENTITY_INSERT [dbo].[ActivityTypeOptions] ON 

INSERT [dbo].[ActivityTypeOptions] ([id], [ActivityName]) VALUES (1, N'Interview Management')
INSERT [dbo].[ActivityTypeOptions] ([id], [ActivityName]) VALUES (2, N'Add Skills Channel')
INSERT [dbo].[ActivityTypeOptions] ([id], [ActivityName]) VALUES (3, N'Update Meeting')
INSERT [dbo].[ActivityTypeOptions] ([id], [ActivityName]) VALUES (4, N'Update Skills Channel')
INSERT [dbo].[ActivityTypeOptions] ([id], [ActivityName]) VALUES (5, N'Create Candidate Feedback')
INSERT [dbo].[ActivityTypeOptions] ([id], [ActivityName]) VALUES (6, N'Update Candidate Feedback')
INSERT [dbo].[ActivityTypeOptions] ([id], [ActivityName]) VALUES (7, N'QC Passed')
INSERT [dbo].[ActivityTypeOptions] ([id], [ActivityName]) VALUES (8, N'QC Failed')
INSERT [dbo].[ActivityTypeOptions] ([id], [ActivityName]) VALUES (9, N'Update ClonableSkillChannel')
INSERT [dbo].[ActivityTypeOptions] ([id], [ActivityName]) VALUES (10, N'Create ClonableSkillChannel')
INSERT [dbo].[ActivityTypeOptions] ([id], [ActivityName]) VALUES (11, N'Meeting Cancelled')
SET IDENTITY_INSERT [dbo].[ActivityTypeOptions] OFF
GO
