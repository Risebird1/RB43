/****** Object:  Table [dbo].[Account]    Script Date: 04-12-2019 6.40.11 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ReliabilityScoreTypes')
BEGIN 

CREATE TABLE [dbo].[ReliabilityScoreTypes](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](MAX) NULL,
	[IsActive] [bit] NOT NULL default 1,
	[PositiveScore] [int] NOT NULL DEFAULT 0,
	[NegativeScore] [int] NOT NULL DEFAULT 0,
 CONSTRAINT [PK_ReliabilityScoreTypes] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]



END

GO




