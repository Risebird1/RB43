IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewer')
BEGIN  
CREATE TABLE [dbo].[TransInterviewer](					
			[id] [int] IDENTITY(1,1) NOT NULL,
			[IRFId] [int] NOT NULL,
			[InterviewerId] [int] NOT NULL,	
            [IsActive] [bit] NOT NULL DEFAULT 1,
            [ClientAction] [Varchar](12) NULL,
            [IsAvailable] [bit] NOT NULL DEFAULT 1,
            [IsSMEVerified] [bit] NULL,
            [Surcharge][int] NULL,
			CONSTRAINT [FK_TIIRFId] FOREIGN KEY (IRFId) REFERENCES IRF(id),
			CONSTRAINT [FK_TI_InterviewerId] FOREIGN KEY (InterviewerId) REFERENCES Interviewer(id),			
 CONSTRAINT [PK_TransInterviewer] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PerSuccessRate')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add PerSuccessRate INT
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsActive')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add IsActive bit not null default 1
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsAvailable')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add IsAvailable bit not null default 1
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ClientAction')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add ClientAction varchar(12)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsSMEVerified')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add IsSMEVerified bit null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Surcharge')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add Surcharge int null
END

END

GO
