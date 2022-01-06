IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewRequests')
BEGIN 

CREATE TABLE [dbo].[InterviewRequests](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[JdTitle] [varchar](50) NULL,
	[JobLevel] int NULL,
	[PrimarySkill] [varchar](200) NULL,
	[JDLink] [varchar](1000) NULL,
	[RequestedBy] [varchar](150) NULL,
	[CreatedOn] [datetime] NULL,
	[ExpectedInterviews] [int] NULL,
	[Country] [varchar](50) NULL,
	[Assignto] [int] NULL,
	[IsIRFCreated] [bit] NULL,
	[IRFId] [varchar](15) NULL,
 CONSTRAINT [PK_InterviewRequests] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IRFId')
      AND Object_ID = Object_ID(N'InterviewRequests'))
BEGIN
    ALTER TABLE InterviewRequests
    Add IRFId varchar(15) NULL
END
GO



