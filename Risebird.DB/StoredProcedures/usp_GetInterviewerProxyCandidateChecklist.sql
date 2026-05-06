IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewerProxyCandidateChecklist'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerProxyCandidateChecklist
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewerProxyCandidateChecklist]
    @InterviewerEmail    VARCHAR(200) = NULL,
    @Id                  INT          = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [Id],
        [InterviewerEmail],
        [ChecklistStateJson],
        [TotalItems],
        [TotalChecked],
        [CreatedBy],
        [CreatedOn]
    FROM [dbo].[InterviewerProxyCandidateChecklist]
    WHERE
        (@Id IS NOT NULL AND [Id] = @Id)
        OR
        (@Id IS NULL AND @InterviewerEmail IS NOT NULL AND [InterviewerEmail] = @InterviewerEmail)
    ORDER BY [CreatedOn] DESC;
END
GO
