/****** Object:  StoredProcedure [dbo].[usp_UpdateMeetingStatus]    Script Date: 18-02-2020 5.11.02 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateMeetingStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateMeetingStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
Create PROCEDURE [dbo].[usp_UpdateMeetingStatus]  
@IRFId nvarchar(100),  
@date DateTime,
@emailid varchar(100)
AS  
Begin  
update IRF  
set IsPaused = 1   
where IRFId=@IRFId  
update InterviewComments set MeetingStatus=null where InterviewId in   
(select id from InterviewMeetings where IRFId=@IRFId and interviewDate>@date)  
  DECLARE @OutputTable Table(
  interviewer varchar(200),
  candidateEmail varchar(200),
  candidateName varchar(200),
  ClientHrContact varchar(200),
  interviewDate datetime,
  imid int,
  IRFId varchar(200)
  );
UPDATE InterviewMeetings  
SET confirmationStatus = 2,Cancellationsource=@emailid,CancellationReason= 'Skill Channel Paused'
OUTPUT inserted.interviewer, inserted.candidateEmail, inserted.candidateName, inserted.ClientHrContact, inserted.interviewDate, inserted.id as imid ,inserted.IRFId
into @OutputTable
WHERE IRFId = @IRFId and interviewDate >= @date  
  
select CASE WHEN ISNULL(ul.IsActive, 0) = 1 THEN ot.interviewer ELSE NULL END as interviewer,
ot.candidateEmail, ot.candidateName, ot.ClientHrContact, ot.interviewDate, ot.imid, ot.IRFId
from @OutputTable ot
left join UserList ul on ul.EmailId = ot.interviewer
End 
  

GO


