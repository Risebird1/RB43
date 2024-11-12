IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewerBillingRate')
BEGIN 
CREATE TABLE [dbo].[InterviewerBillingRate](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NULL,
	[Userid] [int] NULL,
	[BillingRate] [int] NULL,
	[BillingType][varchar](10)
 CONSTRAINT [PK_InterviewerBillingRate] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


