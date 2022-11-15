IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ClientReviewDetails')
BEGIN 
CREATE TABLE [dbo].[ClientReviewDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[ClientEmailid] [varchar](150) NULL,
	[Rating] [int] NULL,
	[Comment] [varchar](1000) NULL,
	[CreatedOn] [datetime] NULL,
 CONSTRAINT [PK_ClientReviewDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


