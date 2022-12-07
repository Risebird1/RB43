IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'CodeReport_TableType')
BEGIN

CREATE TYPE [dbo].[CodeReport_TableType] AS TABLE(
	[inviteId] [varchar](60),
	[submissionId] [varchar](60),
	[questionid] [int],
	[questionName] [varchar](max),
	[questionDescription] [varchar](max),
	[candidateSolution] [varchar](max),
	[achievedScore] [int],
	[questionScore] [int],
	[questionstatus] [varchar](20),
	[input] [varchar](100),
	[expectedOutput] [varchar](100),
	[actualOutput] [varchar](100),
	[testcasestatus] [varchar](20)
)
END
GO


