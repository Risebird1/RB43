IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ClientPremiumUser')
BEGIN  
CREATE TABLE [dbo].[ClientPremiumUser](					
			[id] [int] IDENTITY(1,1) NOT NULL,
			[ClientId] [int] NOT NULL,		
			[UserId] [int] NOT NULL,
			[IsPasswordReset][bit] NULL,
			[MCQValidation][int] NULL,
			CONSTRAINT [FK_CLPrUser_User] FOREIGN KEY (UserId) REFERENCES UserList(id),
			CONSTRAINT [FK_CLPrUser_Client] FOREIGN KEY (ClientId) REFERENCES Client(id),
 CONSTRAINT [PK_ClientPremiumUser] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					
END
GO