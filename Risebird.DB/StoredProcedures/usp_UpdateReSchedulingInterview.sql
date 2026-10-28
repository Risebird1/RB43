IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateReSchedulingInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateReSchedulingInterview
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_UpdateReSchedulingInterview  
@interviewid int,  
@source varchar(100),
@reason varchar(50),
-- Set by the caller only when the candidate themself is rescheduling. Explicit rather than inferred
-- from @source, so a caller that passes a default never burns the candidate's quota. Defaults to 0:
-- every existing caller behaves exactly as before.
@isCandidateInitiated bit = 0
AS
BEGIN
declare @confirmationstatus int
Select @confirmationstatus=confirmationStatus From InterviewMeetings where id=@interviewid
if(@confirmationstatus=1)
begin
Update InterviewMeetings set ConfirmationStatus=2,CancellationSource=@source,CancellationReason=@reason
,CandidateRescheduleCount = CASE WHEN @isCandidateInitiated = 1 THEN ISNULL(CandidateRescheduleCount,0) + 1 ELSE CandidateRescheduleCount END
where id=@interviewid
Select ul.uniqueid,ulh.Emailid as ChannelOwner,im.InterviewDate,im.TimeZonesValue,im.TimeZoneoffset,IRF.id as IRFId,im.ZoomMeetingId   
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join UserList ul on im.candidateEmail=ul.EmailId  
left join UserList ulh on ulh.id=IRF.RisebirdHr  
where im.id=@interviewid
end
else 
begin
select 'NotCancelled' as Message
end
END  
GO