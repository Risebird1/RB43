IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Interviewclearness')
BEGIN 

CREATE TABLE [dbo].[Interviewclearness](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Interviewer] [varchar](200) NULL,
	[CreatedOn] [datetime] NULL,
	[Interviewid] [int] NULL,
	[Action] [varchar](10) NULL,
 CONSTRAINT [PK_Interviewclearness] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
End
GO


