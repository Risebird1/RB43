IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SkillChannelSampleQuestions')
BEGIN  
CREATE TABLE [dbo].[SkillChannelSampleQuestions](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NULL,
	[Skill] [int] NULL,
	[Comment] [varchar](500) NULL,
	[CreatedBy] [varchar](150) NULL,
 CONSTRAINT [PK_SkillChannelSampleQuestions] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


