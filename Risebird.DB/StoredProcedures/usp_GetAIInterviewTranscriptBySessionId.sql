IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAIInterviewTranscriptBySessionId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAIInterviewTranscriptBySessionId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetAIInterviewTranscriptBySessionId
@SessionId varchar(200)
AS
BEGIN

select SessionId, [Role], [Message], [Timestamp] from TranscriptsConv where SessionId = @SessionId order by [Timestamp] desc

END
GO