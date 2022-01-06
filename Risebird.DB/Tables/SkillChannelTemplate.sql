IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SkillChannelTemplate')

BEGIN

CREATE TABLE [dbo].[SkillChannelTemplate](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[TemplateName] [varchar](100) NULL,
	[AssessmentGroupId] [int] NOT NULL,
	[IsDeleted] [bit] NOT NULL default 0,
	[CreatedOn] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[ModifiedOn] [datetime] NULL,
	[ModifiedBy] [int] NULL,
    CONSTRAINT [FK_SkillChannelTemplate_CreatedBy] FOREIGN KEY (CreatedBy) REFERENCES UserList(id),
	CONSTRAINT [FK_SkillChannelTemplate_AssessmentGroupId] FOREIGN KEY (AssessmentGroupId) REFERENCES AssessmentGroup(id),
	CONSTRAINT UQ_SkillChannelTemplate_TemplateName UNIQUE(TemplateName),

 CONSTRAINT [PK_SkillChannelTemplate] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] 

END

GO




