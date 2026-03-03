IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_LogAIInterviewException'))
BEGIN
    DROP PROCEDURE dbo.usp_LogAIInterviewException
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_LogAIInterviewException
    @SessionId             varchar(50),
    @HttpMethod            varchar(10),
    @ExceptionMessage      nvarchar(max),
    @InnerExceptionMessage nvarchar(max),
    @StackTrace            nvarchar(max),
    @LogLevel              int,
    @OccurredAt            datetime
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO AIInterviewExceptionLog
        (SessionId, HttpMethod, ExceptionMessage, InnerExceptionMessage, StackTrace, LogLevel, OccurredAt, IsResolved)
    VALUES
        (@SessionId, @HttpMethod, @ExceptionMessage, @InnerExceptionMessage, @StackTrace, @LogLevel, @OccurredAt, 0)
END
GO
