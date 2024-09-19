/****** Object:  Table [dbo].[PreOnboardingInterviewerAIVerdict]    ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'PreOnboardingInterviewerAIVerdict')
BEGIN
CREATE TABLE [dbo].[PreOnboardingInterviewerAIVerdict](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[EmailId] [varchar](100) NOT NULL,
	[PrimarySpecialization] [varchar](250) NOT NULL,
	[OpenAIResponse] [varchar](max) NOT NULL,
	[AIVerdict] [varchar](50) NOT NULL,
	[CreatedAt][datetime] Null,
	[ModifiedAt][datetime] NULL,
	[CreatedBy][varchar](100) NOT NULL,
	[ModifiedBy][varchar](100) NULL,
 CONSTRAINT [PK_PreOnboardingInterviewerAIVerdict] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] --TEXTIMAGE_ON [PRIMARY]
END

IF EXISTS(
SELECT *
FROM sys.columns
WHERE Name IN (N'EmailId')
    AND Object_ID = Object_ID(N'PreOnboardingInterviewerAIVerdict'))
BEGIN
	ALTER TABLE PreOnboardingInterviewerAIVerdict
	ALTER COLUMN EmailId VARCHAR(100);
END


GO



