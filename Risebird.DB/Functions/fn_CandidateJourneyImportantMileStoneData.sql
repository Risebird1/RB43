
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_CandidateJourneyImportantMileStoneData]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_CandidateJourneyImportantMileStoneData
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create FUNCTION fn_CandidateJourneyImportantMileStoneData(@userId int, @irfId int)  
RETURNS @ResultTable TABLE (AddedOnDate datetime, InvitationSentOnDate datetime, FirstScheduledOnDate datetime null,  FirstScheduledOninterviewDate datetime null)   
AS  
BEGIN  
  

  
DECLARE @TemporaryStatus TABLE (        
   
  DateValue datetime,
  StatusValue int
); 
  
INSERT INTO @TemporaryStatus        
select cs.[Date], cs.[Status] from Candidate ca join CandidatesMaster cm on cm.id = ca.CandidateMasterId 
join CandidateStatus cs on cs.CandidateId = cm.id where ca.IRFId = @irfId and cm.UserId =@userId   
  
declare @AddedOnDate datetime, @InvitationSentOnDate datetime, @FirstScheduledOnDate datetime,  @FirstScheduledOninterviewDate datetime
  
set @AddedOnDate = (select top 1 [DateValue] from @TemporaryStatus where [StatusValue] = 12)  
set @InvitationSentOnDate = @AddedOnDate--(select top 1 [DateValue] from @TemporaryStatus where [StatusValue] = 11 order by [DateValue])   
set @FirstScheduledOnDate = (select top 1 [DateValue] from @TemporaryStatus where [StatusValue] = 4 order by [DateValue])  
set @FirstScheduledOninterviewDate = null--(select top 1 im.interviewDate from Candidate ca join CandidatesMaster cm on cm.id = ca.CandidateMasterId join CandidateStatus cs on cs.CandidateId = cm.id join InterviewMeetings im on im.id = cs.InterviewId where ca.IRFId = @irfId and cm.UserId = @userId and cs.[Status] = 4 order by [Date])  

 
INSERT INTO @ResultTable values (@AddedOnDate, @InvitationSentOnDate, @FirstScheduledOnDate, @FirstScheduledOninterviewDate)
 
return

END  
GO

