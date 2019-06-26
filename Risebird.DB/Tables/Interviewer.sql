IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Interviewer')
BEGIN  
CREATE TABLE [dbo].[Interviewer](					
			[id] [int] IDENTITY(1,1) NOT NULL,			
			[UserId] [int] NOT NULL,					
			[PrimarySkill] [VARCHAR](MAX) NULL,
			[ResumeLink][VARCHAR](MAX) NULL,
			[IsApproved][BIT] NULL,
			[IsActive][bit]NULL,
			[NoOfIntterview][int]NULL,
			[SuccessRate][REAL] null
			CONSTRAINT [FK_IntUser_User] FOREIGN KEY (UserId) REFERENCES UserList(id),			
 CONSTRAINT [PK_Interviewer] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					

END

GO
