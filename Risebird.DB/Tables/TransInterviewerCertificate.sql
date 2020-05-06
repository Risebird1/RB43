IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerCertificate')
BEGIN

CREATE TABLE [dbo].[TransInterviewerCertificate](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerProfileId] [int] NOT NULL,
	[CertificateID] [nvarchar](200) NULL,
	[CertificateName] [nvarchar](200) NULL,
	[Version] [nvarchar](100) NULL,
	[ExpiryDate] [datetime] NULL,
	
	CONSTRAINT [FK__IProfileID_Certificate] FOREIGN KEY (InterviewerProfileId) REFERENCES InterviewerProfile(id),
 CONSTRAINT [PK_TransInterviewerCertificate] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) 
)

END

ELSE
BEGIN

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Version')
      AND Object_ID = Object_ID(N'TransInterviewerCertificate'))
BEGIN
    ALTER TABLE TransInterviewerCertificate
    Add Version nvarchar (100) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ExpiryDate')
      AND Object_ID = Object_ID(N'TransInterviewerCertificate'))
BEGIN
    ALTER TABLE TransInterviewerCertificate
    Add ExpiryDate datetime NULL
END



End