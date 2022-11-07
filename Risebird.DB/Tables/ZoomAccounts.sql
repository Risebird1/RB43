/****** Object:  Table [dbo].[ZoomAccounts]   ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ZoomAccounts')
BEGIN

CREATE TABLE [dbo].[ZoomAccounts](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[UserName] [varchar](50) NULL,
	[ConcurrencyAllowed] int NULL,
	[IsActive] bit NOT NULL Default 1
 CONSTRAINT [PK_ZoomAccounts] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End
GO


