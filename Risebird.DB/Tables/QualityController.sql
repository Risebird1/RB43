IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'QualityController')
BEGIN  
CREATE TABLE [dbo].[QualityController](					
			[id] [int] IDENTITY(1,1) NOT NULL,			
			[UserId] [int] NOT NULL,
			[MobileNumber][BIGINT] NULL,			
			[PrimarySkill] [VARCHAR](MAX) NULL,
			[ResumeLink][VARCHAR](MAX) NULL,
			[IsApproved][BIT] NULL,
			[IsActive][bit]NULL,
			[NoOfIntterview][int]NULL,
			CONSTRAINT [FK_QC_User] FOREIGN KEY (UserId) REFERENCES UserList(id),			
 CONSTRAINT [PK_QualityController] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					

END

GO
