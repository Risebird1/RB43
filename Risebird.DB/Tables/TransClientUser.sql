IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransClientUser')
BEGIN  
CREATE TABLE [dbo].[TransClientUser](					
			[id] [int] IDENTITY(1,1) NOT NULL,
			[IRFId] [int] NOT NULL,
			[ClientUserId] [int] NOT NULL,	
			[ClientRoleId] [int] NOT NULL,							
			[IsActive] [bit] default 1,							
			CONSTRAINT [FK_TCUIRFId] FOREIGN KEY (IRFId) REFERENCES IRF(id),
			CONSTRAINT [FK_TCU_CUserId] FOREIGN KEY (ClientUserId) REFERENCES ClientUser(id),
			CONSTRAINT [FK_TCU_CRoleId] FOREIGN KEY (ClientRoleId) REFERENCES ClientRole(id),
 CONSTRAINT [PK_TransClientUser] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					

END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsActive')
      AND Object_ID = Object_ID(N'TransClientUser'))
BEGIN
    ALTER TABLE TransClientUser
    Add [IsActive] [bit] default 1
END
GO
