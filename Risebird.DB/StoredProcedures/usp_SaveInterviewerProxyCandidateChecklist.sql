IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveInterviewerProxyCandidateChecklist'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewerProxyCandidateChecklist
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SaveInterviewerProxyCandidateChecklist]
    @InterviewerEmail    VARCHAR(200),
    @ChecklistStateJson  NVARCHAR(MAX),
    @TotalItems          INT,
    @TotalChecked        INT,
    @CreatedBy           VARCHAR(200),
    @NewId               INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Prevent duplicates: one row per interviewer — UPDATE if already exists, INSERT otherwise
    IF EXISTS (
        SELECT 1
        FROM [dbo].[InterviewerProxyCandidateChecklist]
        WHERE ISNULL([InterviewerEmail], '') = ISNULL(@InterviewerEmail, '')
    )
    BEGIN
        UPDATE [dbo].[InterviewerProxyCandidateChecklist]
        SET [ChecklistStateJson] = @ChecklistStateJson,
            [TotalItems]         = @TotalItems,
            [TotalChecked]       = @TotalChecked,
            [CreatedBy]          = @CreatedBy,
            [CreatedOn]          = GETUTCDATE()
        WHERE ISNULL([InterviewerEmail], '') = ISNULL(@InterviewerEmail, '');

        SELECT @NewId = [Id]
        FROM [dbo].[InterviewerProxyCandidateChecklist]
        WHERE ISNULL([InterviewerEmail], '') = ISNULL(@InterviewerEmail, '');
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[InterviewerProxyCandidateChecklist]
        (
            [InterviewerEmail],
            [ChecklistStateJson],
            [TotalItems],
            [TotalChecked],
            [CreatedBy],
            [CreatedOn]
        )
        VALUES
        (
            @InterviewerEmail,
            @ChecklistStateJson,
            @TotalItems,
            @TotalChecked,
            @CreatedBy,
            GETUTCDATE()
        );

        SET @NewId = SCOPE_IDENTITY();
    END
END
GO
