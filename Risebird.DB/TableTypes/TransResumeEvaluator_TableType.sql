/****** Object:  UserDefinedTableType [dbo].[TransResumeEvaluator_TableType]    Script Date: 24-06-2020 2.19.56 PM ******/

IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransResumeEvaluator_TableType')
BEGIN

CREATE TYPE [dbo].[TransResumeEvaluator_TableType] AS TABLE(
	[IRFId] [int] NOT NULL,
	[UserId] [int] NOT NULL
)

END
GO


