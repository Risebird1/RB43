IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ClonableSkillChannel')
BEGIN  
CREATE TABLE [dbo].[ClonableSkillChannel](					
			[id] [int] IDENTITY(1,1) NOT NULL,			
			[CreatedFromIRFId] [int] NULL,			
			[Name] [VARCHAR](200) NOT NULL,
			[CreatedOn][DATETIME] NULL,
			[ModifiedOn][DATETIME] NULL,
			[IsJoinSelectGenskillsRequired][bit] NULL,
 CONSTRAINT UC_ClonableSkillChannel_Name UNIQUE ([Name]),
 CONSTRAINT [PK_ClonableSkillChannel] PRIMARY KEY CLUSTERED 					
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
    WHERE Name IN (N'IsJoinSelectGenskillsRequired')
      AND Object_ID = Object_ID(N'ClonableSkillChannel'))
BEGIN
    ALTER TABLE IRF
    Add IsJoinSelectGenskillsRequired BIT NULL
END

END
GO
