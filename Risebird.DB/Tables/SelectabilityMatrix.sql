IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SelectabilityMatrix')
BEGIN  
CREATE TABLE [dbo].[SelectabilityMatrix](					
			[id] [int] IDENTITY(1,1) NOT NULL,
			[Name] [varchar](250) NULL,		
			[Description] [varchar](450) NULL,
			[IsActive] bit 						
 CONSTRAINT [PK_SelectabilityMatrix] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					

END

GO