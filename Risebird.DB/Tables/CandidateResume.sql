IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CandidateResume')
BEGIN
CREATE TABLE [dbo].[CandidateResume](					
			[id] [int] IDENTITY(1,1) NOT NULL,			
			[IRFId] [int] NOT NULL,			
			[ResumeLink] [nvarchar](500) not NULL,
			[AddedBy] [int] not NULL,
			[AddedOn][DateTime] NOT NULL,
			[IsProcessed][bit],
			[IsResumeParsed][bit] null
			CONSTRAINT [FK_CandidateResumes_IRFId] FOREIGN KEY (IRFId) REFERENCES IRF(id),
			CONSTRAINT [FK_CandidateResumes_AddedBy] FOREIGN KEY (AddedBy) REFERENCES UserList(id),
 CONSTRAINT [PK_CandidateResumes] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)	
End

Else
Begin

	if exists (select * from INFORMATION_SCHEMA.TABLE_CONSTRAINTS where CONSTRAINT_NAME = 'FK_CandidateResumes_CandidateId' and TABLE_NAME ='CandidateResume')
		begin
		 alter table CandidateResume
		 drop constraint FK_CandidateResumes_CandidateId
		end
	
	IF  EXISTS(SELECT * FROM sys.columns WHERE Name IN (N'CandidateId')
    AND Object_ID = Object_ID(N'CandidateResume'))
	BEGIN
		alter table CandidateResume
		drop column [CandidateId]
	END

End
IF Not EXISTS(SELECT * FROM sys.columns WHERE Name IN (N'IsProcessed')
    AND Object_ID = Object_ID(N'CandidateResume'))
	BEGIN
		alter table CandidateResume
		Add IsProcessed bit
	END

IF Not EXISTS(SELECT * FROM sys.columns WHERE Name IN (N'IsResumeParsed')
AND Object_ID = Object_ID(N'CandidateResume'))
BEGIN
	alter table CandidateResume
	Add IsResumeParsed bit null
END
Go