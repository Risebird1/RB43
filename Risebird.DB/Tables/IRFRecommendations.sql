IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'IRFRecommendations')
BEGIN 

CREATE TABLE [dbo].[IRFRecommendations](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[CloneIRFId] [int] NOT NULL,
	[CategoryId] [int] NULL,
	[CompetencyLevelId] [int] NULL,
	[SeniorityLevelId] [int] NULL,
	[CountryType] [int] NULL,
	[Status] [bit] NULL,
	[CreatedOn] [datetime] NULL,
	[ModifiedOn] [datetime] NULL,
	[AssessmentGroupId] [int] NULL,
	CONSTRAINT [FK_IRFRecommendations_CategoryId] FOREIGN KEY([CategoryId]) REFERENCES [dbo].[AssessmentGroupCategory] ([id]),
	CONSTRAINT [FK_IRFRecommendations_CloneIRFId] FOREIGN KEY([CloneIRFId]) REFERENCES [dbo].[ClonableSkillChannel] ([id]),
	constraint [FK_IRFRecommendations_AssessmentGroupId] FOREIGN KEY([AssessmentGroupId]) REFERENCES [dbo].[AssessmentGroup] ([id]),
 CONSTRAINT [PK_IRFRecommendations] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END

Go



