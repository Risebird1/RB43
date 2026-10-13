-- Advanced Resume Screening: the screening rubric for one IRF (one row per IRF).
--
-- RubricJson holds the 27-criterion rubric (fixed keys, weights totalling 300) in the shape
-- described by Risebird.Entities.ViewModel.AdvancedScreening.ScreeningRubric. It is the
-- generator's output, what the recruiter edits and approves, and the scorer's input.
--
-- Versions: RubricVersion goes up by one on every approved change and InstructionsVersion on
-- every save of the instructions. Each CandidateAdvancedScreening row records the versions it
-- was scored on, so a lower version there means the result is stale.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'IRFScreeningRubric')
BEGIN
CREATE TABLE [dbo].[IRFScreeningRubric](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	-- Extracted from IRF.JDLink at generation time; editable so a bad extraction can be fixed.
	[JDText] [nvarchar](max) NULL,
	[Instructions] [nvarchar](2000) NULL,
	[InstructionsVersion] [int] NOT NULL CONSTRAINT [DF_IRFScreeningRubric_InstructionsVersion] DEFAULT (0),
	[InstructionsUpdatedBy] [varchar](200) NULL,
	[InstructionsUpdatedOn] [datetime] NULL,
	[ApplyInstructionsToRubric] [bit] NOT NULL CONSTRAINT [DF_IRFScreeningRubric_ApplyToRubric] DEFAULT (1),
	[ApplyInstructionsToScoring] [bit] NOT NULL CONSTRAINT [DF_IRFScreeningRubric_ApplyToScoring] DEFAULT (1),
	[RubricJson] [nvarchar](max) NULL,
	[RubricVersion] [int] NOT NULL CONSTRAINT [DF_IRFScreeningRubric_RubricVersion] DEFAULT (0),
	-- Draft, Generating, Generated, Approved or Failed.
	[Status] [varchar](20) NOT NULL CONSTRAINT [DF_IRFScreeningRubric_Status] DEFAULT ('Draft'),
	[TotalWeight] [int] NULL,
	[LastError] [nvarchar](1000) NULL,
	[GeneratedOn] [datetime] NULL,
	[ApprovedBy] [varchar](200) NULL,
	[ApprovedOn] [datetime] NULL,
	[CreatedBy] [varchar](200) NULL,
	[CreatedOn] [datetime] NULL,
	[ModifiedBy] [varchar](200) NULL,
	[ModifiedOn] [datetime] NULL,
 CONSTRAINT [PK_IRFScreeningRubric] PRIMARY KEY CLUSTERED ([id] ASC),
 CONSTRAINT [UQ_IRFScreeningRubric_IRFId] UNIQUE ([IRFId]),
 CONSTRAINT [FK_IRFScreeningRubric_IRF] FOREIGN KEY ([IRFId]) REFERENCES [dbo].[IRF] ([id])
) ON [PRIMARY]
END
GO
