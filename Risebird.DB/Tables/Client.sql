IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Client')
BEGIN  
CREATE TABLE [dbo].[Client](					
			[id] [int] IDENTITY(1,1) NOT NULL,
			[Name] [varchar](150) NULL,		
			[Description] [varchar](250) NULL,
            [OfficeAddress] [varchar](MAX) NULL,
            [GST] [varchar](MAX) NULL,	
            [LogoLink] [varchar](MAX) NULL,	
            [CompanyLink] [varchar](MAX) NULL,
            [CompanyVideoLink] [varchar](MAX) NULL,
            [IsPasswordReset] [bit] NULL,
            [IsCandidateavailability] [bit] NULL,
            [IsMeetingNotificationSend] [bit] NULL,
            [PerInterviewRate] [int] NULL,
            [BillingType] [int] NULL,
            [InterviewDuration] [int] NULL,
            [IsMeetingAutoCancelled] [bit] NULL,
            [IsQCRequired] [bit] NULL,
            [IsRedoInterview] [bit] NULL,
            [IsClientCancelInterview] [bit] NULL,
            [PaymentTerm] [int] NULL,
            [GSTApplicable] [bit] NULL,
            [ClientType] [bit] NULL, -- service, product, elite
            [CountryType] [int] NULL, -- onsite offshore,
            [Reportsnippet][int] Null,
            [IsTranscript][bit] Null,
            [ShowInterviewInstructions][bit] Null,
            [isCandidateportal][bit] Null,
            [IsPlanFlixable][bit] null,
            [PlanType][int]null,
            [tenantId][varchar](50) NULL,
            [IsVideoRequired][bit] NULL,
            [IsReportwhitelabel][bit] NULL,
            [IsCandidateVerification][bit] NULL,
 CONSTRAINT [PK_Client] PRIMARY KEY CLUSTERED 					
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
    WHERE Name IN (N'GST')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add [GST] [varchar](MAX) NULL		
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'OfficeAddress')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add [OfficeAddress] [varchar](MAX) NULL		
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CuratedLink')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add CuratedLink BIT NOT NULL DEFAULT 0	
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PerSuccessRate')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add PerSuccessRate Int NULL	
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Balance')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add Balance int NULL 	
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'LogoLink')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add [LogoLink] [varchar](MAX) NULL		
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CompanyLink')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add [CompanyLink] [varchar](MAX) NULL		
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CompanyVideoLink')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add [CompanyVideoLink] [varchar](MAX) NULL		
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsPasswordReset')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add [IsPasswordReset] bit	
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsCandidateavailability')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add [IsCandidateavailability] bit	
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsMeetingNotificationSend')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add [IsMeetingNotificationSend] bit	
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PerInterviewRate')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add [PerInterviewRate] int	
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'BillingType')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add [BillingType] int	
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'InterviewDuration')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add [InterviewDuration] int	
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsMeetingAutoCancelled')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add [IsMeetingAutoCancelled] bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsQCRequired')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add IsQCRequired bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsRedoInterview')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add IsRedoInterview bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsClientCancelInterview')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add IsClientCancelInterview bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PaymentTerm')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add PaymentTerm int
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'GSTApplicable')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add GSTApplicable bit
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ClientType')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add ClientType int
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CountryType')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add CountryType int
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Reportsnippet')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add Reportsnippet int
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsTranscript')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add IsTranscript bit
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ShowInterviewInstructions')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add ShowInterviewInstructions bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'isCandidateportal')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add isCandidateportal bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsPlanFlixable')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add IsPlanFlixable bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PlanType')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add PlanType int
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'tenantId')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add tenantId varchar(50)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsVideoRequired')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add IsVideoRequired bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsReportwhitelabel')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add IsReportwhitelabel bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsCandidateVerification')
      AND Object_ID = Object_ID(N'Client'))
BEGIN
    ALTER TABLE Client
    Add IsCandidateVerification bit
END

END

GO
