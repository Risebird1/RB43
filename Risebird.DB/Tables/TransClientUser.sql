IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransClientUser')
BEGIN  
CREATE TABLE [dbo].[TransClientUser](					
			[id] [int] IDENTITY(1,1) NOT NULL,
			[IRFId] [int] NOT NULL,
			[ClientUserId] [int] NOT NULL,	
			[ClientRoleId] [int] NOT NULL,							
			[IsActive] [bit] default 1,							
			[IsAutoMapped] BIT NOT NULL DEFAULT 0,							
			[AutoMappedOn] DATETIME NULL,							
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

-- Column to indicate if this mapping was auto-created by the resume ingestion flow
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name = N'IsAutoMapped'
      AND Object_ID = Object_ID(N'TransClientUser'))
BEGIN
    ALTER TABLE dbo.TransClientUser
    ADD [IsAutoMapped] BIT NOT NULL DEFAULT 0;
END

-- Timestamp of when auto-mapping occurred
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name = N'AutoMappedOn'
      AND Object_ID = Object_ID(N'TransClientUser'))
BEGIN
    ALTER TABLE dbo.TransClientUser
    ADD [AutoMappedOn] DATETIME NULL;
END
GO
