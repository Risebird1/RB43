IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'RaisedIssuesHistory')
BEGIN
CREATE TABLE [dbo].[RaisedIssuesHistory](
	[id]            [int] IDENTITY(1,1) NOT NULL,
	[RaisedIssueId] [int]          NOT NULL,
	[Action]        [varchar](20)  NOT NULL,   -- 'Created' | 'Closed' | 'Reopened'
	[Comment]       [varchar](2000) NULL,
	[ActionBy]      [varchar](200) NOT NULL,   -- email of the person who performed the action
	[ActionByRole]  [varchar](50)  NULL,       -- profile/role at the time of action
	[ActionOn]      [datetime]     NOT NULL,
 CONSTRAINT [PK_RaisedIssuesHistory] PRIMARY KEY CLUSTERED ([id] ASC)
    WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
