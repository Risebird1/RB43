IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchCandidateMeetingsIdForCancellation'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchCandidateMeetingsIdForCancellation
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO    
CREATE Procedure usp_FetchCandidateMeetingsIdForCancellation  
@MeetingPlatForm varchar(50)
As                        
BEGIN                        
if(@MeetingPlatForm='Zoho')                 
begin
Select ZoomMeetingId  
From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId                
where interviewDate>=dateadd(minute,330,GetDate()) and interviewDate<=dateadd(minute,1950,GetDate()) and confirmationstatus=2 and im.MeetingPlatform='Zoho'  
end
else
begin
Select ZoomMeetingId  
From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId                
where interviewDate>=dateadd(minute,330,GetDate()) and interviewDate<=dateadd(minute,1950,GetDate()) and confirmationstatus=2 and ISNULL(IRF.MeetingPlatform,'Zoom')='Zoom'  
end      
END

GO