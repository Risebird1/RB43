IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAIInterviewTranscriptBySessionId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAIInterviewTranscriptBySessionId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetAIInterviewTranscriptBySessionId
@SessionId uniqueidentifier
AS
BEGIN

select SessionId, [Role], [Message], [Timestamp], DeliveryHeadQuestionId, sq.AIQuestionVideoLink 
from TransCriptsConv tc
left join SkillChannelSampleQuestions sq on tc.DeliveryHeadQuestionId = sq.id
where SessionId = @SessionId order by [Timestamp] desc

END
GO