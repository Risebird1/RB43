IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'RecommednedSkillChannel')
BEGIN
CREATE TABLE [dbo].[RecommednedSkillChannel](
       [id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NULL,
	[CreatedBy] [varchar](150) NULL,
	[CreatedOn] [datetime] NULL
) ON [PRIMARY]
END
GO


