IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_InactivateInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_InactivateInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_InactivateInterviewer    
    @interviewerId int,    
    @modifiedBy varchar(200),    
    @modifiedOn datetime,    
    @comment varchar(500),
	@blockinterviewer bit
AS    
BEGIN    
        DECLARE @EmailId varchar(200)    
            
        -- Get the interviewer's email    
        SELECT @EmailId = EmailId     
        FROM UserList     
        WHERE id = @InterviewerId    
    
        -- Update UserList to set IsActive = 0    
        UPDATE UserList     
        SET IsActive = 0    
        WHERE id = @InterviewerId    
    
        -- Update InterviewerProfile flags    
        UPDATE ip    
        SET ModifiedBy = @ModifiedBy,    
            ModifiedOn = @modifiedOn,  
			RedMarkerStatus=3,
			IsDeleted=@blockinterviewer
        FROM InterviewerProfile ip    
        WHERE ip.UserId = @InterviewerId    
    
        -- Unassign upcoming interviews for this interviewer    
        UPDATE InterviewMeetings     
        SET Interviewer = NULL,    
            sModifiedBy = @ModifiedBy,    
            dtModifiedOn = @modifiedOn    
        WHERE     
            Interviewer = @EmailId    
            AND interviewDate > DATEADD(MINUTE, 330, GETDATE())    
    
        -- Update TransInterviewer table    
        UPDATE TransInterviewer     
        SET IsActive = 0,    
            IsAvailable = 0    
        WHERE UserId = @InterviewerId    
       
  --Add Remove Comment for interviewer    
  Insert into RedMarkerStatusDetails (InterviewerUserid,IRFId,Comment,Type,CreatedOn,CreatedBy,Status)    
  values (@InterviewerId,0,@comment,'TotalInterview',@modifiedOn,@modifiedBy,3)    
END    
GO 