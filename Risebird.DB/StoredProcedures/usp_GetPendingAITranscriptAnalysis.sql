IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPendingAITranscriptAnalysis'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPendingAITranscriptAnalysis
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetPendingAITranscriptAnalysis]  
@timeNow datetime
As
Begin

select distinct top 3 im.id, ic.AudioTranscriptFileLink, interviewDate,IRF.id as irfIntId into #TempTable from InterviewComments ic 
join InterviewMeetings im on im.id = ic.InterviewId join IRF on IRF.IRFId=im.IRFId
where AudioTranscriptFileLink is not null and (IsAITranscriptAnalysisAttempted is null or IsAITranscriptAnalysisAttempted = 0) and
(ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and
interviewDate<@timeNow and DATEDIFF(DAY, interviewDate,@timeNow)<7 and ISNULL(im.RedoInterview,0)=0
order by interviewDate desc

update InterviewComments set IsAITranscriptAnalysisAttempted = 1 where InterviewId in (select id from #TempTable)
 
select * from #TempTable

End
GO