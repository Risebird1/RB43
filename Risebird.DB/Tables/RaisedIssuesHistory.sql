IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'RaisedIssuesHistory')
BEGIN
    CREATE TABLE [dbo].[RaisedIssuesHistory] (
        [id]            INT            IDENTITY(1,1) NOT NULL,
        [RaisedIssueId] INT            NOT NULL,
        [Action]        VARCHAR(50)    NOT NULL,       -- 'Created' | 'Closed' | 'Reopened'
        [Comment]       VARCHAR(1000)  NULL,
        [ActionBy]      VARCHAR(200)   NOT NULL,
        [ActionOn]      DATETIME       NOT NULL CONSTRAINT [DF_RaisedIssuesHistory_ActionOn] DEFAULT (GETDATE()),
        CONSTRAINT [PK_RaisedIssuesHistory] PRIMARY KEY CLUSTERED ([id] ASC)
            WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF,
                  IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    )
END
GO
