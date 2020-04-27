IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Client')
BEGIN  
CREATE TABLE [dbo].[Client](					
			[id] [int] IDENTITY(1,1) NOT NULL,
			[Name] [varchar](150) NULL,		
			[Description] [varchar](250) NULL,
            [OfficeAddress] [varchar](MAX) NULL,
            [GST] [varchar](MAX) NULL	
 CONSTRAINT [PK_Client] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					

END

ELSE
BEGIN
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'GST')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add [GST] [varchar](MAX) NULL		
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'OfficeAddress')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add [OfficeAddress] [varchar](MAX) NULL		
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CuratedLink')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add CuratedLink BIT NOT NULL DEFAULT 0	
END
END

GO
