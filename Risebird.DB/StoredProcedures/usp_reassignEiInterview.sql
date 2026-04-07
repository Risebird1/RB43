IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_reassignEiInterview'))
BEGIN
    DROP PROCEDURE [dbo].[usp_reassignEiInterview]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO  
Create Procedure usp_reassignEiInterview  
@meetingid int,  
@interviewer varchar(150)  
As  
BEGIN  
declare @oldinterviewer varchar(150)  
declare @points int  
declare @jobCountry int  
declare @updatetable TABLE (id int)  
if not exists(Select 1 from interviewmeetingsjointime where interviewid=@meetingid)  
begin  
Select @oldinterviewer=Interviewer from InterviewMeetings where id=@meetingid  
Select @jobCountry=JobCountry From IRF join InterviewMeetings im on IRF.IRFId=im.IRFid where im.id=@meetingid  
if(@jobCountry=1)  
begin  
set @points=-200  
end  
else  
begin  
set @points=-350  
end  
--Start: Update EI Interviewer   
update [dbo].[InterviewMeetings] set interviewer=LTrim(RTrim(@interviewer)) Output inserted.id into @updatetable  
where id = @meetingid
--update [dbo].[InterviewMeetings] set interviewer=LTrim(RTrim(@interviewer)),EIAssigned=1,IsEmergency=1 Output inserted.id into @updatetable  
--where id = @meetingid and ISNULL(EIAssigned,0)!=1  
--End: Update EI Interviewer  
Declare @Count int  
set @Count= (Select top 1 id from @updatetable)  
--Start: apply penalty if interviewer is updated   
if(@Count>0)  
begin  
exec usp_applyInterviewerpenalty @meetingid,@points,@oldinterviewer  
end  
--End: apply penalty if interviewer is updated   
end  
Select * from @updatetable  
END  
Go