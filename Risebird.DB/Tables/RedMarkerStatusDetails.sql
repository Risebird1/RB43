IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'RedMarkerStatusDetails')
BEGIN 

CREATE TABLE [dbo].[RedMarkerStatusDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerUserid] [int] NULL,
	[IRFId] [int] NULL,
	[Comment] [varchar](300) NULL,
	[Type] [varchar](30) NULL,
	[CreatedOn] [Datetime] NULL,
	[CreatedBy] [varchar](200) NULL,
	[Status] [int] NULL,
 CONSTRAINT [PK_RedMarkerStatusDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO



