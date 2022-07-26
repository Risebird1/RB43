IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'TransMCQSelectability_TableType')
BEGIN

CREATE TYPE [dbo].[TransMCQSelectability_TableType] AS TABLE(
[MCQId] [varchar](50) NULL,
[CMCQId] [varchar](50) NULL,
[CandidateEmail] [varchar](200) NULL,
[ApplyingCompany] [varchar](50) NULL,
[Type] [varchar](50) NULL,
[Value] [varchar](200) NULL,
[LevelId] [int] NULL,
[RequiredLevelId] [int] NULL
)


End
GO