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
	[IsVerified] [bit] NULL DEFAULT 0,
	[RazorpayValidationId] [varchar](50) NULL,
	CONSTRAINT [FK__IProfileID_Acc] FOREIGN KEY (InterviewerProfileId) REFERENCES InterviewerProfile(id),
 CONSTRAINT [PK_TransInterviewerAccDetails] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
ELSE
BEGIN
	IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'IsVerified' AND Object_ID = Object_ID(N'TransInterviewerAccDetails'))
		ALTER TABLE TransInterviewerAccDetails ADD [IsVerified] [bit] NULL DEFAULT 0
	IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'RazorpayValidationId' AND Object_ID = Object_ID(N'TransInterviewerAccDetails'))
		ALTER TABLE TransInterviewerAccDetails ADD [RazorpayValidationId] [varchar](50) NULL
END
Go

