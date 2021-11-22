IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchAllUnassignedInterviewAutoCancel'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchAllUnassignedInterviewAutoCancel
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO    
            
        

Create Procedure usp_FetchAllUnassignedInterviewAutoCancel
As
BEGIN
declare @tempRecords table (Irfid int,Interviewid int,CandidateEmail varchar(150))   
Insert into @tempRecords
Select IRF.id as Irfid,im.id as Interviewid,candidateEmail as CandidateEmail From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId where interviewDate>=dateadd(minute,330,GetDate()) and interviewDate<=dateadd(minute,420,GetDate())
Update InterviewMeetings set isAutoCancelled=1 where id in(Select Interviewid from @tempRecords)
Select * From @tempRecords
END

GO


