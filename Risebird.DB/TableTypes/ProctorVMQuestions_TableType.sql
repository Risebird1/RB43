/****** Object:  UserDefinedTableType [dbo].[TransVMUser_TableType]    Script Date: 02-10-2020 6.52.08 PM ******/

IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'ProctorVMQuestions_TableType')
BEGIN

CREATE TYPE [dbo].[ProctorVMQuestions_TableType] AS TABLE(
	[VmId] [int] NOT NULL,
	[Priority] [int] NOT NULL,
	[Question] [varchar](max) NOT NULL,
	[Detail] [varchar](max) NULL,
	[VideoLink] [varchar](500) NULL,
	[Time] [int] NULL,
	[Level] [int] NULL,
	[Skill] [varchar](500) NULL
)

END
GO


