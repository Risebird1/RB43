IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewerProfile')
BEGIN
CREATE TABLE [dbo].[InterviewerProfile](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[NickName] [varchar](100) NOT NULL,
	[AltEmailID] [varchar](150) NULL,
	[DOB] [datetime] NULL,
	[Gender] [varchar](50) NULL,
	[AnnualSalary] [nvarchar](200) NULL,
	[Country] [varchar](200) NULL,
	[City] [varchar](200) NULL,
	[AltMobileNumber] [nvarchar](200) NULL,
	[CurrentRole] [varchar](150) NULL,
	[Experience] [decimal](10, 2) NULL,
	[PreferredDomain] [varchar](200) NULL,
	[GitHubLink] [varchar](200) NULL,
	[ResumeLink] [varchar](2000) NULL,
	[CodedResumeLink] [varchar](2000) NULL,
	[StackOverflowLink] [varchar](2000) NULL,
	[LinkedinLink] [varchar](2000) NULL,
	[Blog] [varchar](2000) NULL,
	[IsLeadInterviewer] [bit] NULL DEFAULT 0,
	[IsApproved] [bit] NOT NULL,
	[Isinstaavailable] [bit] NOT NULL,
	[Isavailable] [int] NULL DEFAULT 0,
    [IsNotificationAccepted] [bit] NOT NULL,
    [CreatedOn] [datetime] NULL,
	[ModifiedOn] [datetime] NULL,
	[CreatedBy] [varchar](100) NULL,
	[ModifiedBy] [varchar](100) NULL,
	[IsInterest] [bit] NULL,
	[isVerified] [bit] NULL,
	[FirstTimePopupDisplayed] [bit] NULL,
	[CompanyName] [varchar](100) NULL,
	[IsEmployed] [bit] NULL,
	[EmploymentComment] [varchar](50) NULL,
	[AvailabilityResponseDate] [datetime] NULL,
	[VideoConfirmation] [int] NULL,
	[PaymentSLAConfirmation] [bit] NULL,
	[RedMarker] [int] NULL,
	[RedmarkerAcknowledge] [bit] NULL,
    [VideoLink][varchar](1000) NULL,
    [IsVideoApproved][bit] NUll,
    [LinkAddedOn][datetime] Null,
    [IsResumeRedactAttempted][bit] Null,
    [IsResumeRedactFailed][bit] Null,
    [AssessmentGroupId] [varchar](500) null,
    [ConvertedFromDocResumeLink] [varchar](2000) null,
    [PrimarySpecializationForAcknowledgement] [varchar](250) null,
    [AcknowledgementStatus] [varchar](5) null,
    [RedMarkerStatus] [int] NULL,
    [IsDeleted] [bit] NULL,
	CONSTRAINT [UC_InterviewerProfile] UNIQUE (NickName),
	CONSTRAINT [FK_InterviewerProfile] FOREIGN KEY (UserId) REFERENCES UserList(id),
	CONSTRAINT [PK_InterviewerProfile] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF)
)
 
END

ELSE
BEGIN

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'FirstName')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    DROP COLUMN  FirstName
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'LastName')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    DROP COLUMN  LastName
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'EmailId')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    DROP COLUMN  EmailId
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Location')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    DROP COLUMN  Location
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'MobileNumber')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    DROP COLUMN  MobileNumber
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsActive')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    DROP COLUMN  IsActive
END



IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'DOB')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add DOB datetime NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Gender')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add Gender varchar(50) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AnnualSalary')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add AnnualSalary nvarchar(200) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Country')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add Country varchar(200) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'City')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add City varchar(200) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'GitHubLink')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add GitHubLink nvarchar(max) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'StackOverflowLink')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add StackOverflowLink nvarchar(max) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'LinkedinLink')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add LinkedinLink nvarchar(max) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Blog')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add Blog nvarchar(max) NULL
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AssessmentGroupId')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add AssessmentGroupId varchar(500) 
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Isinstaavailable')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add Isinstaavailable bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Isavailable')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add Isavailable int DEFAULT 0
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsNotificationAccepted')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add IsNotificationAccepted bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CreatedOn')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add CreatedOn datetime
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ModifiedOn')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add ModifiedOn datetime
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CreatedBy')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add CreatedBy varchar(100)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ModifiedBy')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add ModifiedBy varchar(100)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsInterest')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add IsInterest bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'isVerified')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add isVerified bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'FirstTimePopupDisplayed')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add FirstTimePopupDisplayed bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CompanyName')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add CompanyName varchar(100)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsEmployed')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add IsEmployed bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'EmploymentComment')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add EmploymentComment varchar(50)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AvailabilityResponseDate')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add AvailabilityResponseDate datetime
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'VideoConfirmation')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add VideoConfirmation int
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PaymentSLAConfirmation')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add PaymentSLAConfirmation bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'RedMarker')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add RedMarker int
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'RedmarkerAcknowledge')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add RedmarkerAcknowledge bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'VideoLink')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add VideoLink varchar(1000)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsVideoApproved')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add IsVideoApproved bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'LinkAddedOn')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add LinkAddedOn datetime
END

IF Not EXISTS(SELECT * FROM sys.columns WHERE Name IN (N'IsResumeRedactAttempted')
AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
	alter table InterviewerProfile
	Add IsResumeRedactAttempted bit null
END

IF Not EXISTS(SELECT * FROM sys.columns WHERE Name IN (N'IsResumeRedactFailed')
AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
	alter table InterviewerProfile
	Add IsResumeRedactFailed bit null
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ConvertedFromDocResumeLink')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add ConvertedFromDocResumeLink varchar(2000)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PrimarySpecializationForAcknowledgement')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add PrimarySpecializationForAcknowledgement varchar(250)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AcknowledgementStatus')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add AcknowledgementStatus varchar(5)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'RedMarkerStatus')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add RedMarkerStatus int null
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsDeleted')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add IsDeleted bit null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsHoldPayment')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add IsHoldPayment bit null DEFAULT 0
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'HoldPaymentReason')
      AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile
    Add HoldPaymentReason varchar(500) null
END

-- Interviewer service agreement: details the interviewer entered when signing, and the signature image.
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'IsAgreementSigned' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile Add IsAgreementSigned bit not null DEFAULT 0
END

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AgreementVersion' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile Add AgreementVersion varchar(20) null
END

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AgreementSignerName' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile Add AgreementSignerName nvarchar(150) null
END

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AgreementFatherName' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile Add AgreementFatherName nvarchar(150) null
END

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AgreementAddress' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile Add AgreementAddress nvarchar(500) null
END

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AgreementSignedOn' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile Add AgreementSignedOn datetime null
END

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AgreementSignatureUrl' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile Add AgreementSignatureUrl varchar(500) null
END

-- Screenshot of the signing page taken when the interviewer signed (proof of signing).
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AgreementProofUrl' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile Add AgreementProofUrl varchar(500) null
END

End

GO
-- Interview alignment: one-time trial onboarding acknowledgement. The first acknowledgement
-- timestamp is kept; a later acknowledgement never overwrites it.
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TrialOnboardingAcknowledged' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile Add TrialOnboardingAcknowledged bit null
END

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TrialOnboardingAcknowledgedOn' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile Add TrialOnboardingAcknowledgedOn datetime null
END

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TrialOnboardingPolicyVersion' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile Add TrialOnboardingPolicyVersion varchar(50) null
END
GO

-- Interviewer report edit window: one-time acceptance of the report submission / edit policy popup shown
-- on the interviewer dashboard. NULL = popup shows at next login.
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'ReportPolicyAcknowledged' AND Object_ID = Object_ID(N'InterviewerProfile'))
BEGIN
    ALTER TABLE InterviewerProfile Add ReportPolicyAcknowledged bit null
END
GO

-- WhatsApp unsubscribe -> Interviewer Churn Data (dashboard ribbon). Where it came from, e.g.
-- "WhatsApp unsubscribe button (Interviewer Common Interview)" or "WhatsApp Stop reply", the reason the
-- interviewer picked, and when. Availability (Isavailable) and the account are not changed. Cleared when an
-- admin re-enables WhatsApp (/WhatsAppOptOut).
IF COL_LENGTH('dbo.InterviewerProfile', 'ChurnSource') IS NULL
    ALTER TABLE dbo.InterviewerProfile ADD ChurnSource varchar(150) NULL;
GO
IF COL_LENGTH('dbo.InterviewerProfile', 'ChurnReason') IS NULL
    ALTER TABLE dbo.InterviewerProfile ADD ChurnReason nvarchar(300) NULL;
GO
IF COL_LENGTH('dbo.InterviewerProfile', 'ChurnDate') IS NULL
    ALTER TABLE dbo.InterviewerProfile ADD ChurnDate datetime NULL;
GO
