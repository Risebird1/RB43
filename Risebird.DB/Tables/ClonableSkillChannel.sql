IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ClonableSkillChannel')
BEGIN  
CREATE TABLE [dbo].[ClonableSkillChannel](					
			[id] [int] IDENTITY(1,1) NOT NULL,			
			[CreatedFromIRFId] [int] NULL,			
			[Name] [VARCHAR](200) NOT NULL,
			[CreatedOn][DATETIME] NULL,
			[ModifiedOn][DATETIME] NULL,
 CONSTRAINT UC_ClonableSkillChannel_Name UNIQUE ([Name]),
 CONSTRAINT [PK_ClonableSkillChannel] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					

END

GO
