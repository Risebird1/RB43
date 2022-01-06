IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllInstaRequests'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllInstaRequests
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
        
          
          
            
CREATE PROCEDURE [dbo].[usp_GetAllInstaRequests]             
@EmailId varchar(200),               
@Profile varchar(100)          
AS              
Begin             
            
IF (@Profile = 'Client')              
 BEGIN              
 Select ir.*,ag.AssessmentGroup,TotalCount.TotalCandidate,CompletedInterview.CompletedInterview,Seledcted.SelectedCandidate From InstaRequests ir join AssessmentGroup ag on ag.id=ir.AssessmentGroup 
Outer Apply(Select Count(*) as TotalCandidate from InstaInterviewMeetings where RequestId=ir.id) TotalCount
Outer Apply(Select Count(*) as CompletedInterview from InstaInterviewMeetings where RequestId=ir.id and MeetingStatus=4) CompletedInterview
Outer Apply(Select Count(*) as SelectedCandidate from InstaInterviewMeetings where RequestId=ir.id and (InterviewStatus=2 or InterviewStatus=4)) Seledcted
where RequestedBy = @EmailId            
        
 End              
Else            
begin            
Select ir.*,ag.AssessmentGroup,TotalCount.TotalCandidate,CompletedInterview.CompletedInterview,Seledcted.SelectedCandidate From InstaRequests ir join AssessmentGroup ag on ag.id=ir.AssessmentGroup 
Outer Apply(Select Count(*) as TotalCandidate from InstaInterviewMeetings where RequestId=ir.id) TotalCount
Outer Apply(Select Count(*) as CompletedInterview from InstaInterviewMeetings where RequestId=ir.id and MeetingStatus=4) CompletedInterview
Outer Apply(Select Count(*) as SelectedCandidate from InstaInterviewMeetings where RequestId=ir.id and (InterviewStatus=2 or InterviewStatus=4)) Seledcted
End            
End    


GO