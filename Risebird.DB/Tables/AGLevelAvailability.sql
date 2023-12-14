/****** Object:  Table [dbo].[AGLevelAvailability]    Script Date: 11-12-2023 5.14.41 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'AGLevelAvailability')
BEGIN 

Create TABLE [dbo].[AGLevelAvailability](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[AssessmentGroupId] [int] NOT NULL,
	[InterviewerProfileId][int] NOT NULL,
	[IsAvailable] [bit] NULL,
	[Source] [varchar](10) NULL,
	[CreatedBy][varchar](100) NULL,
	[CreatedOn][datetime] NULL,
	[ModifiedBy][varchar](100) NULL,
	[ModifiedOn][datetime] NULL,
	CONSTRAINT [FK_AGLevelAvailability_AssessmentGroupId] FOREIGN KEY (AssessmentGroupId) REFERENCES AssessmentGroup(id),
	CONSTRAINT [FK_AGLevelAvailability_InterviewerProfileId] FOREIGN KEY (InterviewerProfileId) REFERENCES InterviewerProfile(id),
 CONSTRAINT [PK_AGLevelAvailability] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End

GO

