IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Profile')
BEGIN  
CREATE TABLE [dbo].[Profile](					
			[id] [int] IDENTITY(1,1) NOT NULL,
			[Name] [varchar](150) NULL,		
			[Description] [varchar](250) NULL,		
			[Precedence] [int] NULL,
			[IsAdmin] [bit] NULL,	
			[IsActive] [bit] NULL			
 CONSTRAINT [PK_Profile] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					

END

GO
