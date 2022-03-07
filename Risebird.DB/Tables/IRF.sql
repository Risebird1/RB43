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
			[TotalInvites][int] NOT NULL DEFAULT 0,
			[CalendlyLink][VARCHAR](500) NULL,
			[TotalBudget] [int] NULL,
			[CreatedOn][DATETIME] NULL,
			[Discount] [real] NOT NULL,
			[IsBiometricEnabled] [bit] NOT NULL,
			[PriorityLevel] [int] NOT NULL,
			[IsPaused] [bit] NOT NULL,
            [CompanyDetails][VARCHAR](MAX) NULL,
            [Comment][VARCHAR](250) NULL,
            [BussinessType][varchar](4) NULL,
            [Heuristicsscore][int] NULL,
            [idProgress][int] NOT NULL DEFAULT 1,
            [PrimarySkillInvite][varchar](100) NULL,
            [IsQCRequired][bit] NULL,
            [IsJoinabilitySelectabilityRequired][bit] NULL,
            [IsGeneralSkillRequired][bit] NULL,
			CONSTRAINT [FK_IRF_Client] FOREIGN KEY (ClientId) REFERENCES Client(id),
			CONSTRAINT [FK_IRF_User] FOREIGN KEY (RisebirdHr) REFERENCES UserList(id),
 CONSTRAINT [PK_IRF] PRIMARY KEY CLUSTERED 					
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
    WHERE Name IN (N'TotalInvites')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add TotalInvites INT NOT NULL DEFAULT 0
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CalendlyLink')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add CalendlyLink VARCHAR(500)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'TotalBudget')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add TotalBudget int
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CreatedOn')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add CreatedOn Date
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsBiometricEnabled')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsBiometricEnabled BIT NOT NULL DEFAULT 0
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Discount')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add Discount real NOT NULL DEFAULT 0
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PriorityLevel')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add PriorityLevel INT NOT NULL DEFAULT 3
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsPaused')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsPaused BIT NOT NULL DEFAULT 0
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CompanyDetails')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add CompanyDetails [VARCHAR](MAX) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Comment')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add Comment VARCHAR(250)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'idProgress')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
	ADD idProgress int not null default 1
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'BussinessType')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
	ADD BussinessType varchar(4) NULL
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Heuristicsscore')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
	ADD Heuristicsscore int NULL
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PrimarySkillInvite')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add PrimarySkillInvite VARCHAR(100)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'BillingOwner')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add BillingOwner int
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AssessmentGroupId')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add AssessmentGroupId varchar(100)
	END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsQCRequired')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsQCRequired bit
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsJoinabilitySelectabilityRequired')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsJoinabilitySelectabilityRequired bit
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsGeneralSkillRequired')
      AND Object_ID = Object_ID(N'IRF'))
BEGIN
    ALTER TABLE IRF
    Add IsGeneralSkillRequired bit
END

END


GO
