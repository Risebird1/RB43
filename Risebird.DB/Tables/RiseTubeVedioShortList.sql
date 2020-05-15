IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'RiseTubeVedioShortList')
BEGIN  
CREATE TABLE [dbo].[RiseTubeVedioShortList](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NULL,
	[UserId] [int] NULL,
	[ShortListedOn] [datetime] NULL,
	[Status] [varchar](20) NULL,
 CONSTRAINT [PK_RiseTubeVedioShortList] PRIMARY KEY CLUSTERED 
(
	[Id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					

END
ELSE
BEGIN
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ShortListedOn')
      AND Object_ID = Object_ID(N'RiseTubeVedioShortList'))
BEGIN
    ALTER TABLE RiseTubeVedioShortList
    Add ShortListedOn [datetime] null
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Status')
      AND Object_ID = Object_ID(N'RiseTubeVedioShortList'))
BEGIN
    ALTER TABLE RiseTubeVedioShortList
    Add Status [varchar](20) NULL
END
END
GO
