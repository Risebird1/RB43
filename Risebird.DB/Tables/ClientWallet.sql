IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ClientWallet')
BEGIN 
CREATE TABLE [dbo].[ClientWallet](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Clientid] [int] NULL,
	[CreatedBy] [int] NULL,
	[Credit] [int] NULL,
	[Debit] [int] NULL,
	[CreatedOn] [datetime] NULL,
	[Comment] [varchar](max) NULL,
	[Balance] [int] NULL,
 CONSTRAINT [PK_ClientWallet] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


