IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewPoints')
BEGIN

CREATE TABLE [dbo].[InterviewPoints](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Interviewid] [int] NULL,
	[userid] [int] NULL,
	[Points] [int] NULL,
	[Type] [varchar](50) NULL,
	[Comment] [varchar](200) NULL,
	[Status] [varchar](20) NULL,
	[Date] [datetime] NULL,
	[InterviewerId] [int] NULL,
	[PaymentHistoryId] [int] NULL,
 CONSTRAINT [PK_InterviewPoints] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF Not EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'InterviewerId')
      AND Object_ID = Object_ID(N'InterviewPoints'))
BEGIN
    ALTER TABLE InterviewPoints
    Add  InterviewerId int
END
IF Not EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PaymentHistoryId')
      AND Object_ID = Object_ID(N'InterviewPoints'))
BEGIN
    ALTER TABLE InterviewPoints
    Add  PaymentHistoryId int
END
GO

