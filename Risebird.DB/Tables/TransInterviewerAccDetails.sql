IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerAccDetails')
BEGIN
CREATE TABLE [dbo].[TransInterviewerAccDetails](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerProfileId] [int] NOT NULL,
	[AccountName] [varchar](200) NULL,
	[AccountNumber] [nvarchar](200) NULL,
	[IFSCCode] [nvarchar](200) NULL,
	[UPIId] [nvarchar](250) NULL,
	[PAN] [nvarchar](200) NULL,
	[IsBankVerified] [bit] NOT NULL CONSTRAINT DF_TransInterviewerAccDetails_IsBankVerified DEFAULT (0),
	[VerifiedOn] [datetime] NULL,
	[VerifiedAccountNumber] [nvarchar](200) NULL,
	[VerifiedIFSCCode] [nvarchar](200) NULL,
	[VerifiedAccountName] [varchar](200) NULL,
	[NameAtBank] [varchar](200) NULL,
	[CashfreeReferenceId] [bigint] NULL,
	[IsPanVerified] [bit] NOT NULL CONSTRAINT DF_TransInterviewerAccDetails_IsPanVerified DEFAULT (0),
	[PanVerifiedOn] [datetime] NULL,
	[VerifiedPAN] [nvarchar](20) NULL,
	[NameAtPan] [varchar](200) NULL,
	[PanVerificationReferenceId] [bigint] NULL,
	CONSTRAINT [FK__IProfileID_Acc] FOREIGN KEY (InterviewerProfileId) REFERENCES InterviewerProfile(id),
 CONSTRAINT [PK_TransInterviewerAccDetails] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
Go

-- Add PAN verification columns if table already exists (upgrade path)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TransInterviewerAccDetails' AND COLUMN_NAME = 'IsPanVerified')
	ALTER TABLE [dbo].[TransInterviewerAccDetails] ADD [IsPanVerified] [bit] NOT NULL CONSTRAINT DF_TransInterviewerAccDetails_IsPanVerified DEFAULT (0);
Go
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TransInterviewerAccDetails' AND COLUMN_NAME = 'PanVerifiedOn')
	ALTER TABLE [dbo].[TransInterviewerAccDetails] ADD [PanVerifiedOn] [datetime] NULL;
Go
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TransInterviewerAccDetails' AND COLUMN_NAME = 'VerifiedPAN')
	ALTER TABLE [dbo].[TransInterviewerAccDetails] ADD [VerifiedPAN] [nvarchar](20) NULL;
Go
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TransInterviewerAccDetails' AND COLUMN_NAME = 'NameAtPan')
	ALTER TABLE [dbo].[TransInterviewerAccDetails] ADD [NameAtPan] [varchar](200) NULL;
Go
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TransInterviewerAccDetails' AND COLUMN_NAME = 'PanVerificationReferenceId')
	ALTER TABLE [dbo].[TransInterviewerAccDetails] ADD [PanVerificationReferenceId] [bigint] NULL;
Go

