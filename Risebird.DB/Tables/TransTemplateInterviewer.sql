IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransTemplateInterviewer')

BEGIN

CREATE TABLE [dbo].[TransTemplateInterviewer](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[TemplateId] [int] NOT NULL,
	[InterviewerId] [int] NOT NULL,
        [IsSelected] [bit] NOT NULL default 1,
	CONSTRAINT [FK_TransTemplateInterviewer_TemplateId] FOREIGN KEY (TemplateId) REFERENCES SkillChannelTemplate(id),
	CONSTRAINT [FK_TransTemplateInterviewer_InterviewerId] FOREIGN KEY (InterviewerId) REFERENCES InterviewerProfile(id),
 CONSTRAINT [PK_TransTemplateInterviewer] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
GO





