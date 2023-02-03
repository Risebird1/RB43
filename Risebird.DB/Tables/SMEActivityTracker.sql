/****** Object:  Table [dbo].[SMEActivityTracker]    ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SMEActivityTracker')
BEGIN 

CREATE TABLE [dbo].[SMEActivityTracker](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[InterviewerProfileId] [int] NOT NULL,
	[IsJDLinkUploaded] [bit] NULL,
	[AreTechSkillRequirementsUpdated] [bit] NULL,
	[IsAudioCommentRecorded] [bit] NULL,
	[AreInterviewersVerified][bit] NULL,
	[CreatedAt][datetime] NOT NULL,
	[LastUpdatedAt][datetime] NULL
 CONSTRAINT [PK_SMEActivityTracker] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End


GO

