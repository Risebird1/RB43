IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'CandidateResumeUpdate_TableType_Two')
BEGIN
CREATE TYPE [dbo].[CandidateResumeUpdate_TableType_Two] AS TABLE(
	[id] [int],			
	[IsProcessed] [bit] NULL,
	[IsResumeParsed] [bit] NULL,
	[IsResumeParsingFailed] [bit] NULL
)
End
Go