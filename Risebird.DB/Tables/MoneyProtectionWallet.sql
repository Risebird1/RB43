IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'MoneyProtectionWallet')
BEGIN 
CREATE TABLE [dbo].[MoneyProtectionWallet](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Clientid] [int] NULL,
	[Points] [int] NULL,
	[AddedOn] [datetime] NULL,
	[Interviewid] [int] NULL,
	[Comment] [varchar](200) NULL,
	[IsConverted] [bit] NULL,
	[Rate] [int] NULL,
 CONSTRAINT [PK_MoneyProtectionWallet] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[MoneyProtectionWallet]  WITH CHECK ADD  CONSTRAINT [FK_MoneyProtectionWallet] FOREIGN KEY([Clientid])
REFERENCES [dbo].[Client] ([id])

ALTER TABLE [dbo].[MoneyProtectionWallet] CHECK CONSTRAINT [FK_MoneyProtectionWallet]
END
Else 
BEGIN
IF Not EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Rate')
      AND Object_ID = Object_ID(N'MoneyProtectionWallet'))
BEGIN
    ALTER TABLE MoneyProtectionWallet
    Add  Rate int
END
END
GO


