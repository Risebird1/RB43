IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransSecondaryAssessmentGroups') -- the assessment Groups that will become a part of the Master Assessment Group
BEGIN

CREATE TABLE [dbo].[TransSecondaryAssessmentGroups](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MAGId] [int] NOT NULL,
	[CreatedModifiedOn] datetime NULL,
	[IsDeleted] [bit] null,
	CONSTRAINT [FK_TransSecondaryAssessmentGroups_MAGId] FOREIGN KEY([MAGId]) REFERENCES [dbo].[MasterAssessmentGroup] ([id]),
 CONSTRAINT [PK_TransSecondaryAssessmentGroups] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
GO



