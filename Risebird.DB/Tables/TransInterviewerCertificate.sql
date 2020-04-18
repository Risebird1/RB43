IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerCertificate')
BEGIN 


CREATE TABLE [dbo].[TransInterviewerCertificate](
	[Id] [int] IDENTITY(1,1) NOT NULL,  -- PK
	[InterviewerProfileId] [int] NOT NULL, -- FK
	[CertificateID] [nvarchar] (200) NULL,
	[CertificateName ] [nvarchar](200) NULL,
	CONSTRAINT [FK__IProfileID_Certificate] FOREIGN KEY (InterviewerProfileId) REFERENCES InterviewerProfile(id),
 CONSTRAINT [PK_TransInterviewerCertificate] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


END

GO