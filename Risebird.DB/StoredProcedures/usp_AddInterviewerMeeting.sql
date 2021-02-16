    IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInterviewerMeeting'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddInterviewerMeeting]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
    
    
Create PROCEDURE [dbo].[usp_AddInterviewerMeeting] 
@interviewid int,
@ISCId varchar(200),    
@interviewDate varchar(200),    
@interviewSlot varchar(100),    
@interviewDuration int,    
@candidateName varchar(200),    
@candidateEmail varchar(200),    
@ZoomId varchar(200),    
@zoomLink varchar(MAX),    
@comfirmationStatus varchar(100),    
@interviewer varchar(200),  
@sCreatedBy varchar(100),  
@editdate int    
AS    
Begin    
    
IF(@editdate=1)
Begin
Update UnapprovedInterviewerMeetings set [ISCId] = @ISCId,[interviewDate]  =@interviewDate,[interviewSlot] =@interviewSlot 
           ,[interviewDuration] =@interviewDuration,[candidateEmail]  =@candidateEmail,[confirmationStatus]  =@comfirmationStatus
           ,[zoomLink] =@zoomLink ,[ZoomId]  =@ZoomId,[interviewer]  =@interviewer,[sModifiedBy]=@sCreatedBy,[dtModifiedOn] =GETUTCDATE() 
           ,[candidateName]=@candidateName where id=@interviewid
END
   
Else
BEGIN
IF NOT EXISTS (SELECT 1   FROM   UnapprovedInterviewerMeetings    WHERE  candidateEmail = @candidateEmail and interviewDate=@interviewDate)    
Begin    
INSERT INTO [dbo].[UnapprovedInterviewerMeetings]    
            ([ISCId]  
           ,[interviewDate]  
           ,[interviewSlot]  
           ,[interviewDuration]  
           ,[candidateEmail]  
           ,[confirmationStatus]  
           ,[zoomLink]  
           ,[ZoomId]  
           ,[interviewer]  
           ,[HRCoordinatorEmailId]  
           ,[HRComment]  
           ,[HRImageFileURL]  
           ,[sCreatedBy]  
           ,[dtCreatedOn]  
           ,[sModifiedBy]  
           ,[dtModifiedOn]  
           ,[candidateName])  
     VALUES    
           (@ISCId,    
@interviewDate,    
@interviewSlot,    
@interviewDuration,    
@candidateEmail,    
@comfirmationStatus,    
@zoomLink,    
@ZoomId,    
@interviewer,    
'',  
'',  
'',  
@sCreatedBy,    
GETUTCDATE(),  
'System',    
GETUTCDATE(),  
@candidateName  
)    
End 
END
END    
   Go 
    
    