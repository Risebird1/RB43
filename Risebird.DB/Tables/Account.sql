/****** Object:  Table [dbo].[Account]    Script Date: 04-12-2019 6.40.11 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Account')
BEGIN 

CREATE TABLE [dbo].[Account](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](100) NULL,
	[IsDeleted] [bit] NOT NULL default 0,
	[Location] [varchar](100) NULL,
	[ClientId] [int] NOT NULL DEFAULT 0,
	[EndClient] [varchar](100) NULL,
	[AccountOwnerId] [varchar](100) NULL,
	[CreatedOn] [date] NULL,
 CONSTRAINT [PK_Account] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
ELSE
BEGIN

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ClientId')
      AND Object_ID = Object_ID(N'Account'))
BEGIN
    ALTER TABLE Account
    Add ClientId INT NOT NULL DEFAULT 0
END

END
GO




