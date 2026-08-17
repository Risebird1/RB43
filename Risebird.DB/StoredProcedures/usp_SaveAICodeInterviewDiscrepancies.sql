IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveAICodeInterviewDiscrepancies'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveAICodeInterviewDiscrepancies
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Replaces the discrepancy rows for a session and stamps the outcome onto the assignment.
-- Replacing rather than appending is what keeps resubmission from duplicating rows.
Create Procedure usp_SaveAICodeInterviewDiscrepancies
@SessionId varchar(50),
@AssignmentId int = NULL,
@ExpertReportId int = NULL,
@DiscrepanciesJson nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    delete from AICodeInterviewReportDiscrepancy where SessionId = @SessionId;

    if @DiscrepanciesJson is not null and LTRIM(RTRIM(@DiscrepanciesJson)) <> ''
    begin
        insert into AICodeInterviewReportDiscrepancy
            (SessionId, AssignmentId, ExpertReportId, FieldType, QuestionId, DisplayOrder,
             FieldKey, AIValue, ExpertValue, IsDiscrepancy, Severity, CreatedAt)
        select
            @SessionId,
            @AssignmentId,
            @ExpertReportId,
            j.FieldType,
            j.QuestionId,
            j.DisplayOrder,
            j.FieldKey,
            j.AIValue,
            j.ExpertValue,
            isnull(j.IsDiscrepancy, 0),
            isnull(j.Severity, 0),
            GETDATE()
        from OPENJSON(@DiscrepanciesJson)
        with (
            FieldType varchar(50) '$.FieldType',
            QuestionId int '$.QuestionId',
            DisplayOrder int '$.DisplayOrder',
            FieldKey nvarchar(300) '$.FieldKey',
            AIValue nvarchar(max) '$.AIValue',
            ExpertValue nvarchar(max) '$.ExpertValue',
            IsDiscrepancy bit '$.IsDiscrepancy',
            Severity tinyint '$.Severity'
        ) j;
    end

    declare @WasDiscrepancyFound bit = 0, @MaxSeverity tinyint = 0;

    select
        @WasDiscrepancyFound = case when count(1) > 0 then 1 else 0 end,
        @MaxSeverity = isnull(max(Severity), 0)
    from AICodeInterviewReportDiscrepancy
    where SessionId = @SessionId and IsDiscrepancy = 1;

    if @AssignmentId is not null
    begin
        update AICodeInterviewExpertAssignment
        set SubmittedAt = GETDATE(),
            WasDiscrepancyFound = @WasDiscrepancyFound,
            MaxSeverity = @MaxSeverity
        where id = @AssignmentId;
    end

    select @WasDiscrepancyFound as WasDiscrepancyFound, @MaxSeverity as MaxSeverity;
END
GO
