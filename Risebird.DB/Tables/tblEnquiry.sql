IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'tblEnquiry')
BEGIN 
CREATE TABLE [dbo].[tblEnquiry](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](150) NULL,
	[Emailid] [varchar](200) NULL,
	[Designation] [varchar](100) NULL,
	[Phone] [varchar](20) NULL,
	[Company] [varchar](150) NULL,
	[CreatedOn] [datetime] NULL,
	[RequestType] [varchar](50) NULL,
 CONSTRAINT [PK_tblEnquiry] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


