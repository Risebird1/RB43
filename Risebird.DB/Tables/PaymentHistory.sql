IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'PaymentHistory')
BEGIN

CREATE TABLE [dbo].[PaymentHistory](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Userid] [int] NULL,
	[PaymentDate] [datetime] NULL,
	[Amount] [int] NULL,
	[Status] [varchar](10) NULL,
	[Comment] [varchar](100) Null,
	[TotalAmount] [int] NULL,
    [TransferId][varchar](50) NULL,
    [CfTransferId][varchar](100) NULL,
    [PayoutStatus][NVARCHAR](50) NULL,
    [TransferUtr][NVARCHAR](100) NULL,
    [TransactionDate][datetime] NULL,
 CONSTRAINT [PK_PaymentHistory] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Comment')
      AND Object_ID = Object_ID(N'PaymentHistory'))
BEGIN
    ALTER TABLE PaymentHistory
    Add Comment  varchar(100)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'TotalAmount')
      AND Object_ID = Object_ID(N'PaymentHistory'))
BEGIN
    ALTER TABLE PaymentHistory
    Add TotalAmount  int
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name = N'TransferId'
      AND Object_ID = Object_ID(N'PaymentHistory'))
BEGIN
    ALTER TABLE PaymentHistory
    ADD TransferId VARCHAR(50) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name = N'CfTransferId'
      AND Object_ID = Object_ID(N'PaymentHistory'))
BEGIN
    ALTER TABLE PaymentHistory
    ADD CfTransferId VARCHAR(100) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name = N'PayoutStatus'
      AND Object_ID = Object_ID(N'PaymentHistory'))
BEGIN
    ALTER TABLE PaymentHistory
    ADD PayoutStatus NVARCHAR(50) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name = N'TransferUtr'
      AND Object_ID = Object_ID(N'PaymentHistory'))
BEGIN
    ALTER TABLE PaymentHistory
    ADD TransferUtr VARCHAR(100) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name = N'TransactionDate'
      AND Object_ID = Object_ID(N'PaymentHistory'))
BEGIN
    ALTER TABLE PaymentHistory
    ADD TransactionDate DATETIME NULL
END
END
GO


