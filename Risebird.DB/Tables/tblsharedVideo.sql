/****** Object:  Table [dbo].[tblsharedVideo]    Script Date: 5/22/2020 6:05:45 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'tblsharedVideo')
BEGIN 

CREATE TABLE [dbo].[tblsharedVideo](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Userid] [int] NULL,
	[VideoId] [int] NULL,
	[WhoShare] [varchar](100) NULL,
	[SharedOn] [datetime] NULL,
 CONSTRAINT [pk_tblsharedVideo] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)	

END
ELSE
BEGIN
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SharedOn')
      AND Object_ID = Object_ID(N'tblsharedVideo'))
BEGIN
    ALTER TABLE Client
    Add [SharedOn] [datetime] NULL		
END
END
GO

