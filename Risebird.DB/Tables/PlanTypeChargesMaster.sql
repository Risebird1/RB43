/****** Object:  Table [dbo].[PlanTypeChargesMaster]    Script Date: 5/12/2023 2:47:27 AM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'PlanTypeChargesMaster')
BEGIN

CREATE TABLE [dbo].[PlanTypeChargesMaster](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[PlanType] [varchar](25) NOT NULL,
	[Country] [varchar](40) NOT NULL,
	[CountryType] [int] NOT NULL,
	[Rate][int] Not Null,
	[DurationSelectedMin][int] Null,
	[DurationRejectedMin][int] Null,
 CONSTRAINT [PK_PlanTypeChargesMaster] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End
GO

