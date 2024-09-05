
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Country_Classification')
BEGIN
CREATE TABLE [dbo].[Country_Classification](
	[id] [int] NOT NULL,
	[Code] [varchar](10) NULL,
	[Country] [varchar](100) NULL,
	[Classification] [varchar](25) NULL,
	[Location] [varchar](25) NULL,

 CONSTRAINT [PK_Country_Classification] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End


