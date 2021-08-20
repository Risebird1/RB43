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
			[IsLead] bit NULL,
			[IsUnapproved] bit NULL,
			[IsCommon] bit NULL,
			[IsStaffingAgencyAccess] bit null,
 CONSTRAINT [PK_Function] PRIMARY KEY CLUSTERED 					
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
    WHERE Name IN (N'IsLead')
      AND Object_ID = Object_ID(N'Function'))
	BEGIN
		ALTER TABLE [Function]
		Add [IsLead] bit
	END

	IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsUnapproved')
      AND Object_ID = Object_ID(N'Function'))
	BEGIN
		ALTER TABLE [Function]
		Add [IsUnapproved] bit
	END

	IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsCommon')
      AND Object_ID = Object_ID(N'Function'))
	BEGIN
		ALTER TABLE [Function]
		Add [IsCommon] bit
	END

	IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsStaffingAgencyAccess')
      AND Object_ID = Object_ID(N'Function'))
	BEGIN
		ALTER TABLE [Function]
		Add [IsStaffingAgencyAccess] bit
	END

END

GO
