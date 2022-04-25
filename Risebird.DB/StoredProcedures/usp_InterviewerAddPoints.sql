   IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InterviewerAddPoints'))
BEGIN
    DROP PROCEDURE dbo.usp_InterviewerAddPoints
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
    
Create Procedure usp_InterviewerAddPoints         
@interviewId int,        
@UserEmail varchar(200),        
@Type varchar(30),        
@Points int        
AS        
BEGIN        
Declare @Userid int        
Declare @AddPoint int        
Declare @Candidate varchar(200)        
Declare @Count int=0   
Declare @MeetingStatus int
Declare @Irfid int        
Select @Userid=id from UserList where EmailId=@UserEmail        
Select @Irfid=isc.id from UnapprovedInterviewerMeetings im join InterviewerSkillsChannel isc on isc.InterviewerSkillid=im.ISCId where im.id=@interviewId        
Select @MeetingStatus=MeetingStatus from InterviewerComments where InterviewId=@interviewId
Select @Count=count(*) from InterviewPoints where Interviewerid=@interviewId and userid=@Userid and InterviewPoints.Type=@Type       
if(@Count=0)        
Begin        
Select @AddPoint=PerSuccessRate from TransInterviewerSkillsLeadInterviewer where Userid=@Userid and InterviewerSkillId=@Irfid        
set @AddPoint=Round((ISNULL(@AddPoint,0)*ISNULL(@Points,0)/100), 0)    
if(@MeetingStatus=5)
BEGIN
set @AddPoint=-200
END
Select @Candidate=candidateEmail from UnapprovedInterviewerMeetings where id=@interviewId        
Insert into InterviewPoints values(0,@Userid,@AddPoint,@Type,@Candidate,'Pending',GetDate(),@interviewId,0)      
End        
      
      
END 

GO