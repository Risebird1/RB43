IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'CandidateResumeUpdate_TableType_Three')
BEGIN
CREATE TYPE [dbo].[CandidateResumeUpdate_TableType_Three] AS TABLE(
	[id] [int],			
	[IsProcessed] [bit] NULL,
	[IsResumeParsed] [bit] NULL,
	[IsResumeParsingFailed] [bit] NULL,
	[IsEmailMissing] [bit] NULL
)
End
Go