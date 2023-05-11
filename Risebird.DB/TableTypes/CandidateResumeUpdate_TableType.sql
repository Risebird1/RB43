IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'CandidateResumeUpdate_TableType')
BEGIN
CREATE TYPE [dbo].[CandidateResumeUpdate_TableType] AS TABLE(
	[id] [int],			
	[IsProcessed] [bit] NULL,
	[IsResumeParsed] [bit] NULL
)
End
Go