IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SecondaryAssessmentGroups') -- the assessment Groups that will become a part of the Master Assessment Group
BEGIN

CREATE TABLE [dbo].[SecondaryAssessmentGroups](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MAGId] [int] NOT NULL,
	[AssessmentGroupId][int] null,
	[CreatedModifiedOn] datetime NULL,
	[IsDeleted] [bit] null,
	CONSTRAINT [FK_SecondaryAssessmentGroups_MAGId] FOREIGN KEY([MAGId]) REFERENCES [dbo].[MasterAssessmentGroup] ([id]),
	CONSTRAINT [FK_SecondaryAssessmentGroups_AssessmentGroupId] FOREIGN KEY([AssessmentGroupId]) REFERENCES [dbo].[AssessmentGroup] ([id]),
	CONSTRAINT UQ_SecondaryAssessmentGroups_AssessmentGroupId UNIQUE(AssessmentGroupId),

 CONSTRAINT [PK_SecondaryAssessmentGroups] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
GO



