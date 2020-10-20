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
END

ELSE
BEGIN
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Heuristicsscore')
      AND Object_ID = Object_ID(N'ClientJd'))
BEGIN
    ALTER TABLE ClientJd
    Add Heuristicsscore [int] NULL		
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'JDBudget')
      AND Object_ID = Object_ID(N'ClientJd'))
BEGIN
    ALTER TABLE ClientJd
    Add JDBudget [varchar](15) NULL		
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'JobRole')
      AND Object_ID = Object_ID(N'ClientJd'))
BEGIN
    ALTER TABLE ClientJd
    Add JobRole [varchar](50) NULL		
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Location')
      AND Object_ID = Object_ID(N'ClientJd'))
BEGIN
    ALTER TABLE ClientJd
    Add Location [varchar](100) NULL		
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ClientUrl')
      AND Object_ID = Object_ID(N'ClientJd'))
BEGIN
    ALTER TABLE ClientJd
    Add ClientUrl [varchar](max) NULL		
END
END
GO


