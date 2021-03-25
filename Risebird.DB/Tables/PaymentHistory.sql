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
 CONSTRAINT [PK_PaymentHistory] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


