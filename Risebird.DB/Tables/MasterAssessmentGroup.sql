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


-- ---------------------------------------------------------------------------------------------
-- MAG row for the AIInterview catch-all Assessment Group (seeded in AssessmentGroup.sql).
--
-- The AI screening route deliberately bypasses the MAG gate, so it does not need this row to
-- create a channel. It exists so the catch-all behaves like any other Assessment Group on the
-- screens and procedures that INNER JOIN MasterAssessmentGroup — without it, the AG resolves on
-- some surfaces and silently disappears on others.
--
-- Guarded on both unique constraints (UQ_MasterAssessmentGroup_MAGName and
-- UQ_MasterAssessmentGroup_AssessmentGroupId) and on the AG existing, so it is safe to re-run and
-- safe to run before or after AssessmentGroup.sql has been applied.
--
-- Note: usp_GetMAGsWithoutTemplates excludes this MAG explicitly. It must not be offered to
-- "Create Missing Templates" — a curated human-panel template for it would have an empty panel.
-- ---------------------------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM dbo.AssessmentGroup WHERE AssessmentGroup = 'AIInterview')
   AND NOT EXISTS (SELECT 1 FROM dbo.MasterAssessmentGroup WHERE MAGName = 'AIInterview')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.MasterAssessmentGroup mag
        INNER JOIN dbo.AssessmentGroup ag ON ag.id = mag.AssessmentGroupId
        WHERE ag.AssessmentGroup = 'AIInterview')
BEGIN
    INSERT INTO dbo.MasterAssessmentGroup
        ([MAGName], [AssessmentGroupId], [IsDeleted], [CreatedOn], [CreatedBy])
    SELECT TOP 1 'AIInterview', ag.id, 0, GETDATE(), 'system'
    FROM dbo.AssessmentGroup ag
    WHERE ag.AssessmentGroup = 'AIInterview'
    ORDER BY ag.id
END
GO
