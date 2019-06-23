IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Function')
BEGIN  
CREATE TABLE [dbo].[Function](					
			[id] [int] IDENTITY(1,1) NOT NULL,
			[FunctionName] [varchar](150) NULL,		
			[Description] [varchar](250) NULL,		
			[idParentFunction] [int] NULL,		
			[sIcon] [varchar](500) NULL,
			[sURL] [varchar](500) NULL,	
			[iDisplayOrder] int NULL,				
 CONSTRAINT [PK_Function] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					

END

GO
