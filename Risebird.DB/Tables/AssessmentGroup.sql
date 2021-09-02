IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'AssessmentGroup')
BEGIN 

CREATE TABLE [dbo].[AssessmentGroup](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[AssessmentGroup] [varchar](100) NULL,
	[PrimarySkills] [varchar](max) NULL,
    [Isinstaavailable][bit] NULL,
 CONSTRAINT [PK_AssessmentGroup] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Isinstaavailable')
      AND Object_ID = Object_ID(N'AssessmentGroup'))
BEGIN
    ALTER TABLE AssessmentGroup
    Add Isinstaavailable bit NOT NULL DEFAULT 0
END
GO


