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

INSERT [dbo].[ActivityTypeOptions] ([id], [ActivityName]) VALUES (1, N'HR Comment')
INSERT [dbo].[ActivityTypeOptions] ([id], [ActivityName]) VALUES (2, N'Add Skills Channel')
SET IDENTITY_INSERT [dbo].[ActivityTypeOptions] OFF


insert into ActivityTypeOptions values (3,'Update Meeting')
insert into ActivityTypeOptions values (4,'Update Skills Channel')

insert into ActivityTypeOptions values (5,'Create Candidate Feedback')
insert into ActivityTypeOptions values (6,'Update Candidate Feedback')
INSERT [dbo].[ActivityTypeOptions] ([id], [ActivityName]) VALUES (7, N'QC Passed')