IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'InterviewerCodedResumeUpdate_TableType')
BEGIN
CREATE TYPE [dbo].[InterviewerCodedResumeUpdate_TableType] AS TABLE(
	[id] [int],			
	[CodedResumeLink] [varchar](2000) NULL,
	[IsResumeRedactAttempted] [bit] NULL,
	[IsResumeRedactFailed] [bit] NULL
)
End
Go
