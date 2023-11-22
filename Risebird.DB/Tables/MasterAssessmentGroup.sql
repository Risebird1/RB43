IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'MasterAssessmentGroup')

BEGIN

CREATE TABLE [dbo].[MasterAssessmentGroup](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[MAGName] [varchar](100) NULL,--MAG stands for Master Assessment Group
	[IsDeleted] [bit] NOT NULL default 0,
	[CreatedOn] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[ModifiedOn] [datetime] NULL,
	[ModifiedBy] [int] NULL,
	CONSTRAINT UQ_MasterAssessmentGroup_MAGName UNIQUE(MAGName),

 CONSTRAINT [PK_MasterAssessmentGroup] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] 

END

GO




