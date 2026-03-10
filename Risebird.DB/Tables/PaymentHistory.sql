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
	[PayoutReferenceId] [varchar](100) Null,
	[PaymentGateway] [varchar](50) Null,
	[GatewayStatus] [varchar](50) Null,
	[Fees] [int] Null
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
    WHERE Name IN (N'PayoutReferenceId')
      AND Object_ID = Object_ID(N'PaymentHistory'))
BEGIN
    ALTER TABLE PaymentHistory
    Add PayoutReferenceId  varchar(100) NULL
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PaymentGateway')
      AND Object_ID = Object_ID(N'PaymentHistory'))
BEGIN
    ALTER TABLE PaymentHistory
    Add PaymentGateway varchar(50) NULL
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'GatewayStatus')
      AND Object_ID = Object_ID(N'PaymentHistory'))
BEGIN
    ALTER TABLE PaymentHistory
    Add GatewayStatus varchar(50) NULL
END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'Fees' AND Object_ID = Object_ID(N'PaymentHistory'))
BEGIN
    ALTER TABLE PaymentHistory ADD Fees int NULL
END
END
GO



