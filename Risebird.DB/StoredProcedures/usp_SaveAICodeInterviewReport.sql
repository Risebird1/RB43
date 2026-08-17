IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveAICodeInterviewReport'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveAICodeInterviewReport
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Upserts one copy of an AI code interview report and replaces its per question rows.
-- @ReportSource 0 stores the AI baseline (written once, when the audit is first opened),
-- 1 stores the expert copy (rewritten on every resubmission).
Create Procedure usp_SaveAICodeInterviewReport
@SessionId varchar(50),
@InterviewId int = NULL,
@ReportSource tinyint,
@OverallVerdict nvarchar(200) = NULL,
@OverallComment nvarchar(max) = NULL,
@OverallCommunicationRating decimal(5,2) = NULL,
@OverallCommunicationComment nvarchar(max) = NULL,
@RubricRatingsJson nvarchar(max) = NULL,
@QuestionRatingsJson nvarchar(max) = NULL,
@User varchar(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    declare @ReportId int;

    select @ReportId = id from AICodeInterviewReport
    where SessionId = @SessionId and ReportSource = @ReportSource;

    if @ReportId is null
    begin
        insert into AICodeInterviewReport
            (SessionId, InterviewId, ReportSource, OverallVerdict, OverallComment,
             OverallCommunicationRating, OverallCommunicationComment, RubricRatingsJson,
             CreatedAt, CreatedBy, ModifiedAt, ModifiedBy)
        values
            (@SessionId, @InterviewId, @ReportSource, @OverallVerdict, @OverallComment,
             @OverallCommunicationRating, @OverallCommunicationComment, @RubricRatingsJson,
             GETDATE(), @User, GETDATE(), @User);

        set @ReportId = SCOPE_IDENTITY();
    end
    else
    begin
        update AICodeInterviewReport
        set InterviewId = isnull(@InterviewId, InterviewId),
            OverallVerdict = @OverallVerdict,
            OverallComment = @OverallComment,
            OverallCommunicationRating = @OverallCommunicationRating,
            OverallCommunicationComment = @OverallCommunicationComment,
            RubricRatingsJson = @RubricRatingsJson,
            ModifiedAt = GETDATE(),
            ModifiedBy = @User
        where id = @ReportId;
    end

    -- Per question rows are replaced wholesale so a resubmission cannot leave stale rows behind.
    delete from AICodeInterviewReportQuestionRating where ReportId = @ReportId;

    if @QuestionRatingsJson is not null and LTRIM(RTRIM(@QuestionRatingsJson)) <> ''
    begin
        insert into AICodeInterviewReportQuestionRating
            (ReportId, QuestionId, BotInterviewQuestionVideoId, DisplayOrder, QuestionText,
             Rating, Verdict, Comment, CommunicationRating, CommunicationComment,
             RubricRatingsJson, TranscriptText)
        select
            @ReportId,
            j.QuestionId,
            j.BotInterviewQuestionVideoId,
            j.DisplayOrder,
            j.QuestionText,
            j.Rating,
            j.Verdict,
            j.Comment,
            j.CommunicationRating,
            j.CommunicationComment,
            j.RubricRatingsJson,
            j.TranscriptText
        from OPENJSON(@QuestionRatingsJson)
        with (
            QuestionId int '$.QuestionId',
            BotInterviewQuestionVideoId int '$.BotInterviewQuestionVideoId',
            DisplayOrder int '$.DisplayOrder',
            QuestionText nvarchar(max) '$.QuestionText',
            Rating decimal(5,2) '$.Rating',
            Verdict nvarchar(200) '$.Verdict',
            Comment nvarchar(max) '$.Comment',
            CommunicationRating decimal(5,2) '$.CommunicationRating',
            CommunicationComment nvarchar(max) '$.CommunicationComment',
            RubricRatingsJson nvarchar(max) '$.RubricRatingsJson',
            TranscriptText nvarchar(max) '$.TranscriptText'
        ) j;
    end

    select @ReportId as ReportId;
END
GO
