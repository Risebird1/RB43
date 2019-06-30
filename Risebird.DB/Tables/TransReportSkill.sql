IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransReportSkill')
BEGIN  
CREATE TABLE [dbo].[TransReportSkill](					
			[id] [int] IDENTITY(1,1) NOT NULL,
			[idReport] [int] NOT NULL,
			[idSkill] [int] NOT NULL,			
			[Details] [varchar](MAX) NULL,				
			CONSTRAINT [FK_TRSRPId] FOREIGN KEY (idReport) REFERENCES Report(id),
			CONSTRAINT [FK_TRSkillId] FOREIGN KEY (idSkill) REFERENCES Skill(id),
 CONSTRAINT [PK_TransReportSkill] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					

END

GO
