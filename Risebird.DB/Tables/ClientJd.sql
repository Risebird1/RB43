IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ClientJd')
BEGIN 
CREATE TABLE [dbo].[ClientJd](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[StartDate] [datetime] NULL,
	[EndDate] [datetime] NULL,
	[JobDescription] [varchar](max) NULL,
	[AssessmentGroupid] [int] NULL,
	[Createdby] [int] NULL,
	[Craetedon] [datetime] NULL,
	[Clientid] [int] NULL,
	[JDTitle] [varchar](200) NULL,
	[Filelink] [varchar](max) NULL,
 CONSTRAINT [PK_ClientJd] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


