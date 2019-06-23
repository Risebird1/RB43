IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ProfileFunction')
BEGIN  
CREATE TABLE [dbo].[ProfileFunction](					
			[id] [int] IDENTITY(1,1) NOT NULL,
			[idProfile] [int] NOT NULL,		
			[idFunction] [int] NOT NULL,	
			[dAccess] [bit] NOT NULL
					
 CONSTRAINT [PK_ProfileFunction] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					

END

GO
