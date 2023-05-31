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
	[PlanTypeEnumValue][int] Null,
	[droppedSorRRate][int] Null,
 CONSTRAINT [PK_PlanTypeChargesMaster] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End
Else
Begin
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PlanTypeEnumValue')
      AND Object_ID = Object_ID(N'PlanTypeChargesMaster'))
BEGIN
    ALTER TABLE PlanTypeChargesMaster
    Add PlanTypeEnumValue INT 
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'droppedSorRRate')
      AND Object_ID = Object_ID(N'PlanTypeChargesMaster'))
BEGIN
    ALTER TABLE PlanTypeChargesMaster
    Add droppedSorRRate INT 
END
End
GO




GO
SET IDENTITY_INSERT [dbo].[PlanTypeChargesMaster] ON 

INSERT [dbo].[PlanTypeChargesMaster] ([id], [PlanType], [Country], [Rate], [CountryType], [DurationSelectedMin], [DurationRejectedMin],[droppedSorRRate]) VALUES (1, N'Standard', N'India', 350, 1, 35, 25,60)
INSERT [dbo].[PlanTypeChargesMaster] ([id], [PlanType], [Country], [Rate], [CountryType], [DurationSelectedMin], [DurationRejectedMin],[droppedSorRRate]) VALUES (2, N'Standard', N'Outside India', 500, 2, 35, 25,60)
INSERT [dbo].[PlanTypeChargesMaster] ([id], [PlanType], [Country], [Rate], [CountryType], [DurationSelectedMin], [DurationRejectedMin],[droppedSorRRate]) VALUES (3, N'Pro', N'India', 420, 1, 45, 35,60)
INSERT [dbo].[PlanTypeChargesMaster] ([id], [PlanType], [Country], [Rate], [CountryType], [DurationSelectedMin], [DurationRejectedMin],[droppedSorRRate]) VALUES (4, N'Pro', N'Outside India', 840, 2, 45, 35,60)
INSERT [dbo].[PlanTypeChargesMaster] ([id], [PlanType], [Country], [Rate], [CountryType], [DurationSelectedMin], [DurationRejectedMin],[droppedSorRRate]) VALUES (5, N'Advanced', N'India', 500, 1, 55, 45,60)
INSERT [dbo].[PlanTypeChargesMaster] ([id], [PlanType], [Country], [Rate], [CountryType], [DurationSelectedMin], [DurationRejectedMin],[droppedSorRRate]) VALUES (6, N'Advanced', N'Outside India', 1000, 2, 55, 45,60)
SET IDENTITY_INSERT [dbo].[PlanTypeChargesMaster] OFF
GO
