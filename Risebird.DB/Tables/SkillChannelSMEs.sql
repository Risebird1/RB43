/****** Object:  Table [dbo].[SkillChannelSMEs]    Script Date: 13-01-2021 5.14.41 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SkillChannelSMEs')
BEGIN 

CREATE TABLE [dbo].[SkillChannelSMEs](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[InterviewerProfileId] [int] NOT NULL,
	[AddedBy] [varchar](100) NOT NULL,
	[DeletedBy] [varchar](100) NULL,
	[IsDeleted] [bit] NULL,
	[PerformedAt] [datetime] NOT NULL,
	
 CONSTRAINT [PK_SkillChannelSMEs] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End

GO

