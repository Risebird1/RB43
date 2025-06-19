IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'MasterAssessmentGroup')

BEGIN

CREATE TABLE [dbo].[MasterAssessmentGroup](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[MAGName] [varchar](100) NULL,--MAG stands for Master Assessment Group
	[AssessmentGroupId][int] null,
	[IsDeleted] [bit] NULL,
	[CreatedOn] [datetime] NOT NULL,
	[CreatedBy] [varchar](100) NULL,
	[ModifiedOn] [datetime] NULL,
	[ModifiedBy] [varchar](100) NULL,
	[MAGLinkEmailTriggeredBy] [varchar](100) NULL,
	[MAGLinkEmailTriggeredOn] [datetime](100) NULL,
	CONSTRAINT [FK_MasterAssessmentGroup_AssessmentGroupId] FOREIGN KEY([AssessmentGroupId]) REFERENCES [dbo].[AssessmentGroup] ([id]),
	CONSTRAINT UQ_MasterAssessmentGroup_MAGName UNIQUE(MAGName),
	CONSTRAINT UQ_MasterAssessmentGroup_AssessmentGroupId UNIQUE(AssessmentGroupId),

 CONSTRAINT [PK_MasterAssessmentGroup] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] 

END
ELSE
BEGIN
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'MAGLinkEmailTriggeredBy')
      AND Object_ID = Object_ID(N'MasterAssessmentGroup'))
BEGIN
    ALTER TABLE MasterAssessmentGroup
    Add [MAGLinkEmailTriggeredBy] varchar(100)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'MAGLinkEmailTriggeredOn')
      AND Object_ID = Object_ID(N'MasterAssessmentGroup'))
BEGIN
    ALTER TABLE MasterAssessmentGroup
    Add [MAGLinkEmailTriggeredOn] datetime
END

End
GO

