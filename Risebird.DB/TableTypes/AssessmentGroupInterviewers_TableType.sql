
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = '_TableType')
BEGIN

CREATE TYPE [dbo].[AssessmentGroupInterviewers_TableType] AS TABLE(
	[AssessmentGroupId] [int] NOT NULL,
	[InterviewerProfileId][int] NOT NULL,
	[IsSME] [bit] NULL,
	[CreatedBy][varchar](100) NOT NULL,
	[CreatedOn][datetime] NULL,
	[ModifiedBy][varchar](100) NULL,
	[ModifiedAt][datetime] NULL
)

END
GO
