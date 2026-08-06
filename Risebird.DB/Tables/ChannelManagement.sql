IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ChannelManagement')
BEGIN  
CREATE TABLE [dbo].[ChannelManagement](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Client] [varchar](100) NULL,
	[PrimarySkill] [varchar](50) NULL,
	[EndClient] [varchar](100) NULL,
	[Location] [varchar](200) NULL,
	[ExpectedVolume] [int] NULL,
	[JdLink] [varchar](max) NULL,
	[WeekendDrive] [bit] NULL,
	[POGenerated] [bit] NULL,
	[IPSCreated] [bit] NULL,
	[CalendlyLinkCreated] [bit] NULL,
	[CandidateSourcing] [bit] NULL,
	[AdditionalComment] [varchar](max) NULL,
	[HRCoordinator] [varchar](150) NULL,
	[ProgramManager] [varchar](150) NULL,
	[IRFID] [bit] NULL,
	[IRFIDs] [varchar](200) NULL,
	[Priority] [varchar](5) NULL,
	[CreatedBy] [varchar](150) NULL,
	[CreatedDate] [datetime] NULL,
	[Status] [varchar](10) NULL,
	[RequesterEmailid] [varchar](150) NULL,
	[RequesterMobile] [varchar](14) NULL,
	[Requester] [varchar](100) NULL,
	[Title] [varchar](50) NULL,
	[SeniorityLevel] [varchar](50) NULL,
	[TurnAroundTimeindays] [varchar](50) NULL,
	[Comments] [varchar](max) NULL,
	[NumberofInterviews] [int] NULL,
	[Origin] [varchar](15) NULL,
	[EngagementType] [varchar](5) NULL,
	[Stage] [varchar](5) NULL,
	[ExpectedPrice] [int] NULL,
	[Expectedrevenue] [int] NULL,
	[PaymentStatus] [varchar](5) NULL,
	[ConfirmationStatus] [varchar](5) NULL,
	[ClientId] [int] NULL,
	[PlanType] [int] NULL,
	[ClientType] [int] NULL,
	[RequesterType] [varchar](15) NULL,
	[MAGId] [int] NULL,
	[TemplateId] [int] NULL,
	[ChannelOwner] [varchar](150) NULL,
 CONSTRAINT [PK_ChannelManagement] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

END
ELSE
BEGIN

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ClientId')
      AND Object_ID = Object_ID(N'ChannelManagement'))
BEGIN
    ALTER TABLE ChannelManagement
    ADD ClientId int NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PlanType')
      AND Object_ID = Object_ID(N'ChannelManagement'))
BEGIN
    ALTER TABLE ChannelManagement
    ADD PlanType int NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ClientType')
      AND Object_ID = Object_ID(N'ChannelManagement'))
BEGIN
    ALTER TABLE ChannelManagement
    ADD ClientType int NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'MAGId')
      AND Object_ID = Object_ID(N'ChannelManagement'))
BEGIN
    ALTER TABLE ChannelManagement
    ADD MAGId int NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'TemplateId')
      AND Object_ID = Object_ID(N'ChannelManagement'))
BEGIN
    ALTER TABLE ChannelManagement
    ADD TemplateId int NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'RequesterType')
      AND Object_ID = Object_ID(N'ChannelManagement'))
BEGIN
    ALTER TABLE ChannelManagement
    ADD RequesterType varchar(15) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ChannelOwner')
      AND Object_ID = Object_ID(N'ChannelManagement'))
BEGIN
    ALTER TABLE ChannelManagement
    ADD ChannelOwner varchar(150) NULL
END

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'Origin' AND Object_ID = Object_ID(N'ChannelManagement')
      AND max_length > 0 AND max_length < 15)
BEGIN
    ALTER TABLE ChannelManagement ALTER COLUMN Origin varchar(15) NULL;
END

END
GO



