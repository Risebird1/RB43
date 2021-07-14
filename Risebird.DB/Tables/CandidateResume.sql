IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CandidateResume')
BEGIN
CREATE TABLE [dbo].[CandidateResume](					
			[id] [int] IDENTITY(1,1) NOT NULL,			
			[CandidateId] [int] NOT NULL,
			[IRFId] [int] NOT NULL,			
			[ResumeLink] [nvarchar](500) not NULL,
			[AddedBy] [int] not NULL,
			[AddedOn][DateTime] NOT NULL
			CONSTRAINT [FK_CandidateResumes_CandidateId] FOREIGN KEY (CandidateId) REFERENCES CandidatesMaster(id),
			CONSTRAINT [FK_CandidateResumes_IRFId] FOREIGN KEY (IRFId) REFERENCES IRF(id),
			CONSTRAINT [FK_CandidateResumes_AddedBy] FOREIGN KEY (AddedBy) REFERENCES UserList(id),
 CONSTRAINT [PK_CandidateResumes] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)	
End
Go