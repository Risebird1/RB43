
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_CandidateLatestStatusForIrfStatsForExcel]')
                      AND type IN ( N'TF' ))
BEGIN
    DROP FUNCTION dbo.fn_CandidateLatestStatusForIrfStatsForExcel
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create FUNCTION fn_CandidateLatestStatusForIrfStatsForExcel(@userId int, @irfId int, @timeNow datetime)      
RETURNS @ResultTable TABLE (LatestStatus varchar(20), interviewId INT null, interviewDate datetime null, completedDate datetime null,
							addedOn datetime, statusUpdatedAt datetime)  --varchar(20)     
AS      
BEGIN      
      
declare @candidateEmail varchar(150), @irfName varchar(150)      
      
set @candidateEmail = (select EmailId from UserList where id = @userId)      
set @irfName = (select IRFId from IRF where id = @irfId)      
      
DECLARE @TemporaryStatus TABLE (            
  confirmationStatus int,      
  MeetingStatus int,      
  InterviewStatus int,      
  interviewDate datetime,    
  interviewId int null    
);     
      
INSERT INTO @TemporaryStatus            
select top 1 cast(im.confirmationStatus as int), ic.MeetingStatus, ic.InterviewStatus, im.interviewDate,  im.id     
from InterviewMeetings im       
left join InterviewComments ic on im.id=ic.InterviewId       
where im.candidateEmail=@candidateEmail and im.IRFId = @irfName and ISNULL(im.IsDeleted,0)=0      
--and cast(im.interviewDate as date) >= @startDate        
order by im.interviewDate desc      
      
declare @confirmationStatus int, @meetingStatus int, @interviewStatus int, @interviewDate datetime,     
@interviewId int, @result varchar(20),@IsColdCandidate bit, @addedOn datetime, @statusUpdatedAt datetime        
      
set @confirmationStatus = (select top 1 confirmationStatus from @TemporaryStatus)      
set @meetingStatus = (select top 1 MeetingStatus from @TemporaryStatus)      
set @interviewStatus = (select top 1 InterviewStatus from @TemporaryStatus)      
set @interviewDate = (select top 1 interviewDate from @TemporaryStatus)      
set @interviewId = (select top 1 interviewId from @TemporaryStatus)   

Select @IsColdCandidate=IsColdCandidate from ColdCandidateInfos cci join Candidate ca on ca.id=cci.FKCandidateId    
join CandidatesMaster cm on cm.id=ca.CandidateMasterId    
where cm.UserId=@userId and ca.IRFId=@irfId    

set @addedOn = (select top 1 DATEADD(minute, 330, [Date]) from CandidateStatus where CandidateId = (select id from CandidatesMaster where UserId = @userId) and [Status] = 12)  


--Cold Candidate      
if (@IsColdCandidate=1)          
begin          
  set @result = 'COLDCANDIDATE'          
 INSERT INTO @ResultTable (LatestStatus, interviewId, interviewDate, completedDate, addedOn, statusUpdatedAt)    
 SELECT @result, null, null, null,  @addedOn, (select top 1 DATEADD(minute, 330, [Date]) from CandidateStatus where CandidateId = (select id from CandidatesMaster where UserId = @userId) and [Status] = 9)      
end 
--case no entry in interviewmeetings table      
else if not exists(select top 1 confirmationStatus from @TemporaryStatus)      
begin      
  set @result = 'NOTSCHEDULED'      
  INSERT INTO @ResultTable (LatestStatus, interviewId, interviewDate, completedDate, addedOn, statusUpdatedAt)    
  SELECT @result, null, null, null, @addedOn, @addedOn        
end      
      
--selected condition      
else if(@confirmationStatus=1 and @interviewStatus in (2))      
begin      
 set @result = 'STRONGLY SELECTED'     
 INSERT INTO @ResultTable (LatestStatus, interviewId, interviewDate, completedDate, addedOn, statusUpdatedAt)    
 SELECT @result, @interviewId, @interviewDate, (select top 1 DATEADD(minute, 330, [Date]) from CandidateStatus where InterviewId = @interviewId and [Status]=14 and Comments = 'Completed' order by id desc), @addedOn, (select top 1 DATEADD(minute, 330, [Date]) from CandidateStatus where InterviewId = @interviewId and [Status]=14 and Comments = 'Completed' order by id desc)    
end      
else if(@confirmationStatus=1 and @interviewStatus in (4))      
begin      
 set @result = 'MARGINALLY SELECTED'     
 INSERT INTO @ResultTable (LatestStatus, interviewId, interviewDate, completedDate, addedOn, statusUpdatedAt)    
 SELECT @result, @interviewId, @interviewDate, (select top 1 DATEADD(minute, 330, [Date]) from CandidateStatus where InterviewId = @interviewId and [Status]=14 and Comments = 'Completed' order by id desc),@addedOn, (select top 1 DATEADD(minute, 330, [Date]) from CandidateStatus where InterviewId = @interviewId and [Status]=14 and Comments = 'Completed' order by id desc)     
end  
--case for rejected      
else if(@confirmationStatus=1 and @interviewStatus in (1,3))    --here status 1 stands for proxy, we are counting it in 'strongly rejected' for the sake of simplifying  
begin      
  set @result = 'STRONGLY REJECTED'      
  INSERT INTO @ResultTable (LatestStatus, interviewId, interviewDate, completedDate, addedOn, statusUpdatedAt)    
  SELECT @result, @interviewId, @interviewDate, (select top 1 DATEADD(minute, 330, [Date]) from CandidateStatus where InterviewId = @interviewId and [Status]=14 and Comments = 'Completed' order by id desc), @addedOn, (select top 1 DATEADD(minute, 330, [Date]) from CandidateStatus where InterviewId = @interviewId and [Status]=14 and Comments = 'Completed' order by id desc)     
end   
else if(@confirmationStatus=1 and @interviewStatus in (5))      
begin      
  set @result = 'MARGINALLY REJECTED'      
  INSERT INTO @ResultTable (LatestStatus, interviewId, interviewDate, completedDate, addedOn, statusUpdatedAt)    
  SELECT @result, @interviewId, @interviewDate, (select top 1 DATEADD(minute, 330, [Date]) from CandidateStatus where InterviewId = @interviewId and [Status]=14 and Comments = 'Completed' order by id desc)  , @addedOn, (select top 1 DATEADD(minute, 330, [Date]) from CandidateStatus where InterviewId = @interviewId and [Status]=14 and Comments = 'Completed' order by id desc)   
end       
--case for eligibility for rescheduling      
else if(@confirmationStatus = 2 or @meetingStatus in (1,3,5,9))      
begin      
 set @result = 'RESCHEDULE'      
 INSERT INTO @ResultTable (LatestStatus, interviewId, interviewDate, completedDate, addedOn, statusUpdatedAt)    
 SELECT @result, @interviewId, @interviewDate, null, @addedOn, (select top 1 DATEADD(minute, 330, [Date]) from CandidateStatus where interviewId = @interviewId and [Status] = 14 order by [Date] desc)       
end      
      
--case for scheduled in the future      
else if(@interviewDate> @timeNow and @confirmationStatus = 1)      
begin      
 set @result = 'FUTURESCHEDULE'      
 INSERT INTO @ResultTable (LatestStatus, interviewId, interviewDate , completedDate, addedOn, statusUpdatedAt)    
 SELECT @result, @interviewId, @interviewDate, null, @addedOn, (select top 1 DATEADD(minute, 330, [Date]) from CandidateStatus where interviewId = @interviewId and [Status] = 4 order by [Date] desc)        
end      
      
--case for report pending      
else if(@interviewDate < @timeNow and @confirmationStatus = 1 and (@meetingStatus=2 or @meetingStatus is null))      
begin      
 set @result = 'PENDING'     
 INSERT INTO @ResultTable (LatestStatus, interviewId, interviewDate , completedDate, addedOn, statusUpdatedAt)    
 SELECT @result, @interviewId, @interviewDate, null, @addedOn, (select top 1 DATEADD(minute, 330, [Date]) from CandidateStatus where interviewId = @interviewId and [Status] = 4)      
end      
      
return    
    
END      
GO

