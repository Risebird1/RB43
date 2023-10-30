/****** Object:  Table [dbo].[ZoomApiTokenMaster]   ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ZoomApiTokenMaster')
BEGIN 

CREATE TABLE [dbo].[ZoomApiTokenMaster](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Token] [nvarchar](max) NULL,
	[Created_ModifiedOn] [datetime] NULL, --ist
 CONSTRAINT [PK_ZoomApiTokenMaster] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END

GO




