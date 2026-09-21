IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'TransInterviewerAccDetailsHistory')

BEGIN
CREATE TABLE [dbo].[TransInterviewerAccDetailsHistory](
	[Id] [int] NULL,
	[InterviewerProfileId] [int] NULL,
	[AccountName] [varchar](200) NULL,
	[AccountNumber] [nvarchar](200) NULL,
	[IFSCCode] [nvarchar](200) NULL,
	[UPIId] [nvarchar](250) NULL,
	[PAN] [nvarchar](200) NULL,
	[IsBankVerified] [bit] NULL,
	[VerifiedOn] [datetime] NULL,
	[VerifiedAccountNumber] [nvarchar](200) NULL,
	[VerifiedIFSCCode] [nvarchar](200) NULL,
	[VerifiedAccountName] [varchar](200) NULL,
	[NameAtBank] [varchar](200) NULL,
	[CashfreeReferenceId] [bigint] NULL,
	[IsPanVerified] [bit] NULL,
	[PanVerifiedOn] [datetime] NULL,
	[VerifiedPAN] [nvarchar](20) NULL,
	[NameAtPan] [varchar](200) NULL,
	[PanVerificationReferenceId] [bigint] NULL,
	[PerformedAt] [datetime] NULL,
	[PerformedBy] [varchar](100) NULL,
	[ActionType] [varchar](15) NULL)


End

GO
