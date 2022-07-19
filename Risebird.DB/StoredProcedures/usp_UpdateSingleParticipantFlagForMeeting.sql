
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateSingleParticipantFlagForMeeting'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateSingleParticipantFlagForMeeting
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_UpdateSingleParticipantFlagForMeeting]
@currentTime datetime
 
AS
BEGIN

begin try
	begin tran
		declare @interviewTime datetime
		select @interviewTime = dateadd(minute, datediff(minute,0,GETDATE()) / 15 * 15, 0) -- setting interviewTime to the nearest 15 minute interval before the current time

		;WITH FirstCTE 
		AS
		(
			select im.id, Meetingid
			from ZoomMeetingTransmission zmt
			join InterviewMeetings im on im.ZoomMeetingId = zmt.Meetingid  
			where im.interviewDate = @interviewTime
			and Event = 'PARTICIPANT_JOINED'
			group by Meetingid, im.id, ParticipantName
		),
		SecondCTE 
		AS 
		(
		  select Meetingid, ParticipantName,
		  ROW_NUMBER() over (partition by Meetingid order by meetingId) as rowNumber
		  from ZoomMeetingTransmission 
		  where  meetingId in 
		  (
		  select Meetingid
		  from FirstCTE 
		  )
		  and Event = 'PARTICIPANT_JOINED'
		  group by Meetingid, ParticipantName 
		)
		SELECT distinct FirstCTE.id into #TempInterviewMeetingIds FROM FirstCTE 
		where FirstCTE.Meetingid not in (select distinct MeetingId from SecondCTE where rowNumber > 1)
		
		update InterviewMeetings set HasDistinctParticipants = 0 
		where id in (select id from #TempInterviewMeetingIds)

		drop table #TempInterviewMeetingIds

	commit tran

end try
begin catch  
  
  rollback transaction  
  
  DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),  
   @Severity int = ERROR_SEVERITY(),  
   @State smallint = ERROR_STATE()  
   
  RAISERROR (@Message, @Severity, @State)  
   
end catch



END
GO


