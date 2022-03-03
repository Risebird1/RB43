IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'IRFRecommendations')
BEGIN 

CREATE TABLE [dbo].[IRFRecommendations](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[CategoryId] [int] NULL,
	[CompetencyLevelId] [int] NULL,
	[SeniorityLevelId] [int] NULL,
	[CountryType] [int] NULL,
	[Status] [bit] NULL,
	[CreatedOn] [datetime] NULL,
	[ModifiedOn] [datetime] NULL,
	CONSTRAINT [FK_IRFRecommendations_CategoryId] FOREIGN KEY([CategoryId]) REFERENCES [dbo].[AssessmentGroupCategory] ([id]),
	CONSTRAINT [FK_IRFRecommendations_IRFId] FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),
 CONSTRAINT [PK_IRFRecommendations] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
Go



