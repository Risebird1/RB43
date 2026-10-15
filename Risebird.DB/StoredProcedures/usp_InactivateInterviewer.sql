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
	@blockinterviewer bit,
    @InactivationType varchar(50) = NULL   -- when NULL, falls back to ManualBlock/ManualRemove (legacy behaviour)
AS    
BEGIN    
        DECLARE @EmailId varchar(200)    
            
        -- Get the interviewer's email    
        SELECT @EmailId = EmailId     
        FROM UserList     
        WHERE id = @InterviewerId    
    
        -- Resolve the inactivation type: explicit value wins, else legacy block/remove default.
        DECLARE @ResolvedType varchar(50) =
            ISNULL(@InactivationType, CASE WHEN @blockinterviewer = 1 THEN 'ManualBlock' ELSE 'ManualRemove' END)

        -- Update UserList to set IsActive = 0 with inactivation tracking
        UPDATE UserList     
        SET IsActive = 0,
            InactivationType = @ResolvedType,
            InactivatedBy = @ModifiedBy,
            InactivatedAt = @modifiedOn
        WHERE id = @InterviewerId    
        -- RowsChanged (returned at the end): 0 = unknown interviewer, nothing was changed.
        DECLARE @RowsChanged int = @@ROWCOUNT
    
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
       
  --Add Remove Comment for interviewer (CreatedOn in IST, like every other RedMarkerStatusDetails writer)
  IF @RowsChanged > 0
  Insert into RedMarkerStatusDetails (InterviewerUserid,IRFId,Comment,Type,CreatedOn,CreatedBy,Status)    
  values (@InterviewerId,0,LEFT(@comment, 300),'TotalInterview',DATEADD(MINUTE, 330, GETUTCDATE()),@modifiedBy,3)

  -- QC Activity: who blocked / inactivated the interviewer (skipped while the log table is missing).
  IF @RowsChanged > 0 AND OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL
  BEGIN
      INSERT INTO dbo.InterviewerActionLog (InterviewerUserId, InterviewerEmail, ActionType, Reason, ActedBy, Source)
      VALUES (@InterviewerId,
              @EmailId,
              CASE WHEN @blockinterviewer = 1 THEN 'Blocked' ELSE 'Inactivated' END,
              LEFT(@ResolvedType + ISNULL(': ' + NULLIF(LTRIM(RTRIM(@comment)), ''), ''), 500),
              NULLIF(LTRIM(RTRIM(@modifiedBy)), ''),
              'usp_InactivateInterviewer')
  END

  SELECT @RowsChanged AS RowsChanged
END
GO 