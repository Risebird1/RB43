IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewerPersonal')
BEGIN  
CREATE TABLE [dbo].[InterviewerPersonal](					
			[id] [int] IDENTITY(1,1) NOT NULL,			
			[UserId] [int] NOT NULL,
			[NickName][varchar](50) NOT NULL,
			[AltEmailId][varchar](150) NULL,
			[Location][varchar](150) NULL,
			[AltMobileNumber][varchar](150) NULL,
			[CurrentRole][varchar](100) NOT NULL,
			[Experience] [varchar](50) NOT NULL,
			[CurrentDomain][varchar](100) NOT NULL,
			[ResumeLink][VARCHAR](MAX) NOT NULL,
			[CodedResumeLink][varchar](MAX) NOT NULL,			
			[IsActive][bit]NULL,
			[IsApproved][bit] null
			CONSTRAINT [FK_InterviewerPersonal_User] FOREIGN KEY (UserId) REFERENCES UserList(id),			
 CONSTRAINT [PK_InterviewerPersonal] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					

END

GO
