IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewer')
BEGIN  
CREATE TABLE [dbo].[TransInterviewer](					
			[id] [int] IDENTITY(1,1) NOT NULL,
			[IRFId] [int] NOT NULL,
			[InterviewerId] [int] NOT NULL,	
            [IsActive] [bit] NULL,
            [ClientAction] [Varchar](12) NULL,
            [IsAvailable] [bit] NOT NULL DEFAULT 1,
            [IsSMEVerified] [bit] NULL,
            [Surcharge] [int] NULL,
            [Source] [varchar](75) null,
            [ModifiedBy][varchar](75) null,
            [ActivatedAt][datetime] null,
            [IsCampaignActivity][bit] null,
            [CampaignId][int] null,
            [OpenAIScreeningResponse][varchar](max) null,
            [AIVerdict][varchar](20) null,
            [AddedAt][datetime] null,
            [ModifiedAt][datetime] null,
            [AIScreeningScore][int] null,
			CONSTRAINT [FK_TIIRFId] FOREIGN KEY (IRFId) REFERENCES IRF(id),
			CONSTRAINT [FK_TI_InterviewerId] FOREIGN KEY (InterviewerId) REFERENCES Interviewer(id),
            [IsSurchargeApprove] [bit] NULL,
            [IsAIScreeningAtempted] [bit] NULL,
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
    Add IsActive bit 
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

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Source')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add Source varchar(75) null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ModifiedBy')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add ModifiedBy varchar(75) null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ActivatedAt')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add ActivatedAt datetime null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsCampaignActivity')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add IsCampaignActivity bit null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CampaignId')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add CampaignId int null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsSurchargeApprove')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add IsSurchargeApprove bit null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'OpenAIScreeningResponse')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add OpenAIScreeningResponse varchar(max) null
END


IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AIVerdict')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add AIVerdict varchar(20) null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsAIScreeningAtempted')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add IsAIScreeningAtempted bit null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AddedAt')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add AddedAt datetime null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ModifiedAt')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add ModifiedAt datetime null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AIScreeningScore')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add AIScreeningScore int null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'InterviewCapacity')
      AND Object_ID = Object_ID(N'TransInterviewer'))
BEGIN
    ALTER TABLE TransInterviewer
    Add InterviewCapacity int NOT NULL CONSTRAINT DF_TransInterviewer_InterviewCapacity DEFAULT 0
END

END

GO
