IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'RiskCategory')
BEGIN

CREATE TABLE [dbo].[RiskCategory](
	[Id]          [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NOT NULL,
	[Name]        [varchar](max) NULL,
	[Description] [varchar](500) NULL,
	[CreatedOn]   [datetime] NOT NULL CONSTRAINT [DF_RiskCategory_CreatedOn] DEFAULT (GETDATE()),
	[ModifiedOn]  [datetime] NULL,
 CONSTRAINT [PK_RiskCategory] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_RiskCategory_InterviewId] UNIQUE NONCLUSTERED ([InterviewId] ASC)
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

END
ELSE
BEGIN

IF NOT EXISTS(
    SELECT * FROM sys.columns
    WHERE Name = N'Description'
      AND Object_ID = OBJECT_ID(N'RiskCategory'))
BEGIN
    ALTER TABLE RiskCategory ADD Description varchar(500) NULL
END

IF NOT EXISTS(
    SELECT * FROM sys.columns
    WHERE Name = N'ModifiedOn'
      AND Object_ID = OBJECT_ID(N'RiskCategory'))
BEGIN
    ALTER TABLE RiskCategory ADD ModifiedOn datetime NULL
END

END
GO
