IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewerCertificates')
BEGIN

CREATE TABLE [dbo].[InterviewerCertificates](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[interviewerid] [int] NULL,
	[year] [varchar](15) NULL,
	[month] [varchar](15) NULL,
	[certificatelink] [varchar](1000) NULL,
	[comment] [varchar](250) NULL,
 CONSTRAINT [PK_InterviewerCertificates] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


