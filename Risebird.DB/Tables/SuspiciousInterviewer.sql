IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SuspiciousInterviewer')
BEGIN
CREATE TABLE [dbo].[SuspiciousInterviewer](
	[Id] [int] IDENTITY(1,101) NOT NULL,
	[UserId] [int] NULL,
	[Comment] [varchar](100) NULL,
	[Createddate] [datetime] NULL,
	[CreatedBy] [varchar](50) NULL,
	[SuspiciousStatus] [bit] NULL,
 CONSTRAINT [PK_InterviewSuspicious] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
--IF NOT EXISTS(
--    SELECT *
--    FROM sys.columns
--    WHERE Name IN (N'SuspiciousInterviewer')
--      AND Object_ID = Object_ID(N'SuspiciousInterviewer'))
--BEGIN
--    ALTER TABLE AadhaarDetails
--    Add matchPercentage varchar(8)
--END
--IF NOT EXISTS(
--    SELECT *
--    FROM sys.columns
--    WHERE Name IN (N'user_image')
--      AND Object_ID = Object_ID(N'AadhaarDetails'))
--BEGIN
--    ALTER TABLE AadhaarDetails
--    Add user_image varchar(max)
--END
GO


