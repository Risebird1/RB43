IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddorUpdateCallRecordingInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_AddorUpdateCallRecordingInterviewer
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_AddorUpdateCallRecordingInterviewer  
@RecordingUrl varchar(1000),  
@Sid varchar(50),  
@CandidateEmail varchar(150),  
@WhoCalled varchar(150)  
AS  
BEGIN  
if not Exists (Select 1 from CallRecordingInterviewer where Sid=@Sid)  
BEGIN  
Insert into CallRecordingInterviewer(Interviewer,RecordingUrl,Sid,WhoCalled,CalledOn)     
values(@CandidateEmail,@RecordingUrl,@Sid,@WhoCalled,dateadd(minute,330,GetDate()))    
END  
Else  
Begin  
Update CallRecordingInterviewer set RecordingUrl=@RecordingUrl where Sid=@Sid  
End  
  
END
GO