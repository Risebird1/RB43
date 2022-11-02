IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'AadhaarDetails')
BEGIN
CREATE TABLE [dbo].[AadhaarDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](100) NULL,
	[Address] [varchar](max) NULL,
	[email] [varchar](200) NULL,
	[reference_id] [varchar](50) NULL,
	[gender] [varchar](1) NULL,
	[dob] [varchar](20) NULL,
	[doc_face] [varchar](max) NULL,
	[verified_using] [varchar](20) NULL,
	[CreatedOn] [datetime] NULL,
	[matchPercentage] [varchar](8) NULL,
	[user_image] [varchar](max) NULL
 CONSTRAINT [PK_AadhaarDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'matchPercentage')
      AND Object_ID = Object_ID(N'AadhaarDetails'))
BEGIN
    ALTER TABLE AadhaarDetails
    Add matchPercentage varchar(8)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'user_image')
      AND Object_ID = Object_ID(N'AadhaarDetails'))
BEGIN
    ALTER TABLE AadhaarDetails
    Add user_image varchar(max)
END
GO


