IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'PageClickDetails')
BEGIN 
CREATE TABLE [dbo].[PageClickDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[currenturl] [varchar](500) NULL,
	[Functiontype] [varchar](100) NULL,
	[ButtonAction] [varchar](50) NULL,
	[PerformedBy] [varchar](150) NULL,
	[PerformedOn] [datetime] NULL,
	[ProfileType] [varchar](50) NULL,
 CONSTRAINT [PK_PageClickDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


