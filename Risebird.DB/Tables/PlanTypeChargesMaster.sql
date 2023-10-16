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
	[CompanyType][int] Null,
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

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CompanyType')
      AND Object_ID = Object_ID(N'PlanTypeChargesMaster'))
BEGIN
    ALTER TABLE PlanTypeChargesMaster
    Add CompanyType int
END

End
GO


GO
SET IDENTITY_INSERT [dbo].[DynamicMarginsByTime] ON 

INSERT [dbo].[DynamicMarginsByTime] ([id], [Day], [WeekDay], [Session1], [Session2], [Session3], [Session4]) VALUES (1, N'Mon', 1, 20, 30, 40, 50)
INSERT [dbo].[DynamicMarginsByTime] ([id], [Day], [WeekDay], [Session1], [Session2], [Session3], [Session4]) VALUES (2, N'Tue', 2, 20, 30, 40, 50)
INSERT [dbo].[DynamicMarginsByTime] ([id], [Day], [WeekDay], [Session1], [Session2], [Session3], [Session4]) VALUES (3, N'Wed', 3, 20, 30, 40, 50)
INSERT [dbo].[DynamicMarginsByTime] ([id], [Day], [WeekDay], [Session1], [Session2], [Session3], [Session4]) VALUES (4, N'Thu', 4, 20, 30, 40, 50)
INSERT [dbo].[DynamicMarginsByTime] ([id], [Day], [WeekDay], [Session1], [Session2], [Session3], [Session4]) VALUES (5, N'Fri', 5, 20, 30, 40, 50)
INSERT [dbo].[DynamicMarginsByTime] ([id], [Day], [WeekDay], [Session1], [Session2], [Session3], [Session4]) VALUES (6, N'Sat', 6, 20, 30, 40, 50)
INSERT [dbo].[DynamicMarginsByTime] ([id], [Day], [WeekDay], [Session1], [Session2], [Session3], [Session4]) VALUES (7, N'Sun', 7, 20, 30, 40, 50)

SET IDENTITY_INSERT [dbo].[DynamicMarginsByTime] OFF
GO
GO
