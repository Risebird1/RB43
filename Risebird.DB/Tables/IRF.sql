IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'IRF')
BEGIN  
CREATE TABLE [dbo].[IRF](					
			[id] [int] IDENTITY(1,1) NOT NULL,			
			[Name] [VARCHAR](200) NOT NULL,
			[ClientId] [int] NOT NULL,			
			[SkillCluster] [VARCHAR](200) NULL,
			[Location] [VARCHAR](200) NULL,
			[StartDate][DateTime] NOT NULL,
			[EndDate][DateTime] NOT NULL,
			[JDLink][VARCHAR](MAX) NULL,
			[NoOfPosition] [int] NULL,	
			[NoOfIntterview][int] NULL,
			[RisebirdHr] [int] NOT NULL,	
			CONSTRAINT [FK_IRF_Client] FOREIGN KEY (ClientId) REFERENCES Client(id),
			CONSTRAINT [FK_IRF_User] FOREIGN KEY (RisebirdHr) REFERENCES UserList(id),
 CONSTRAINT [PK_IRF] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					

END

GO
