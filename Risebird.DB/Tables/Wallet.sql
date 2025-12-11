IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Wallet')
BEGIN 
CREATE TABLE [dbo].[Wallet](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[BillingType] [int] NOT NULL, -- 1: Billing Owner (Client Users), 3: Client
	[ClientId] [int] NULL,
	[BillingOwnerId] [int] NULL, -- UserList.id for Billing Owner
	[CreatedBy] [int] NULL,
	[Credit] [int] NULL,
	[Debit] [int] NULL,
	[CreatedOn] [datetime] NULL,
	[Comment] [varchar](max) NULL,
	[Balance] [int] NULL,
	[InterviewId] [int] NULL, -- Reference to InterviewMeetings.id
	[IRFId] [int] NULL, -- Reference to IRF.id
	[TransactionType] [varchar](50) NULL, -- 'Manual', 'InterviewPoints', 'InterviewBilling'
 CONSTRAINT [PK_Wallet] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

-- Foreign Key Constraints
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Client]') AND type in (N'U'))
BEGIN
	ALTER TABLE [dbo].[Wallet]  WITH CHECK ADD  CONSTRAINT [FK_Wallet_Client] FOREIGN KEY([ClientId])
	REFERENCES [dbo].[Client] ([id])
	ALTER TABLE [dbo].[Wallet] CHECK CONSTRAINT [FK_Wallet_Client]
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserList]') AND type in (N'U'))
BEGIN
	ALTER TABLE [dbo].[Wallet]  WITH CHECK ADD  CONSTRAINT [FK_Wallet_BillingOwner] FOREIGN KEY([BillingOwnerId])
	REFERENCES [dbo].[UserList] ([id])
	ALTER TABLE [dbo].[Wallet] CHECK CONSTRAINT [FK_Wallet_BillingOwner]
END

END
GO

