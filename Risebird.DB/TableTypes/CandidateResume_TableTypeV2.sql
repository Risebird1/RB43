IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'CandidateResume_TableTypeV2')
BEGIN
CREATE TYPE [dbo].[CandidateResume_TableTypeV2] AS TABLE(
	[IRFId] [int] NOT NULL,			
	[ResumeLink] [nvarchar](500) not NULL,
	[AddedBy] [int] not NULL,
	[AddedOn][DateTime] NOT NULL,
	[ClientHREmailId][varchar](100) null
)
End
Go