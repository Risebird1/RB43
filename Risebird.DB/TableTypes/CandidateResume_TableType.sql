IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'CandidateResume_TableType')
BEGIN
CREATE TYPE [dbo].[CandidateResume_TableType] AS TABLE(
	[IRFId] [int] NOT NULL,			
	[ResumeLink] [nvarchar](500) not NULL,
	[AddedBy] [int] not NULL,
	[AddedOn][DateTime] NOT NULL
)
End
Go