IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ClientUser')
BEGIN  
CREATE TABLE [dbo].[ClientUser](					
			[id] [int] IDENTITY(1,1) NOT NULL,
			[ClientId] [int] NOT NULL,		
			[UserId] [int] NOT NULL,
			[RoleId] [int] NOT NULL,
			[Description] [int] NOT NULL,
			[IsPasswordReset][bit] NULL,
			[IsActive][bit] default 1,
			CONSTRAINT [FK_CLUser_User] FOREIGN KEY (UserId) REFERENCES UserList(id),
			CONSTRAINT [FK_CLUser_Client] FOREIGN KEY (ClientId) REFERENCES Client(id),
 CONSTRAINT [PK_ClientUser] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					

END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsPasswordReset')
      AND Object_ID = Object_ID(N'ClientUser'))
BEGIN
    ALTER TABLE ClientUser
    Add [IsPasswordReset] [bit] NULL
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsActive')
      AND Object_ID = Object_ID(N'ClientUser'))
BEGIN
    ALTER TABLE ClientUser
    Add [IsActive] [bit] default 1
END
GO
