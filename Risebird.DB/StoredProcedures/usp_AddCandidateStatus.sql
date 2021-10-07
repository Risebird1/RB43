/****** Object:  StoredProcedure [dbo].[usp_AddCandidateStatus]    Script Date: 24-11-2020 2.34.29 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddCandidateStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_AddCandidateStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


  
  
Create PROCEDURE [dbo].[usp_AddCandidateStatus]    
@IRFId int,    
@CandidateId int,    
@Comments varchar(max),    
@Date datetime,    
@AudioFileLink nvarchar(max),    
@Status int,    
@PerformedBy varchar(100),    
@InterviewId int    
        
AS    
BEGIN    
    
If(@InterviewId is null)    
    
Begin    
 INSERT INTO CandidateStatus (IRFId, CandidateId, AudioFileLink, Comments, [Date], [Status], [PerformedBy])    
    VALUES (@IRFId, @CandidateId, @AudioFileLink, @Comments, GETDATE(), @Status, @PerformedBy)    
    
     
end    
    
Else    
Begin    
 declare @IsAdd bit, @existingComment varchar(100), @existingInterviewConfirmationStatus int    
 set @IsAdd=1    
 --check if the last status for the same type in mEeetingStatus or INterviewStatus or Qc Pending was the same, i.e. 14,15,19    
 if(@Status in (14,15))    
 begin  
 set @existingComment = (select top 1 Comments from CandidateStatus where CandidateId=@CandidateId and IRFId =@IRFId and InterviewId =@InterviewId and Status = @Status order by id desc)    
  if(@existingComment = @Comments)    
  begin    
  set @IsAdd=0    
  end    
 end  
   
 else if(@Status = 19)    
 begin    
 declare @QCStatus int    
 set @QCStatus = (select QCStatus from QCFeedbackReport where InterviewId =@InterviewId)    
    
 if(@QCStatus is null)--QC is actually pending    
  begin    
    
   if exists(select top 1 id from CandidateStatus where CandidateId=@CandidateId and IRFId =@IRFId and InterviewId =@InterviewId and Status = 19 order by id desc)    
    begin    
     set @IsAdd=0    
    end    
      
  End    
    
 Else-- QC status already has a value, therefore it is not pending anymore    
  Begin    
   set @IsAdd=0    
  End    
    
 End    
    
    
 else if(@Status in (18,4))    
 begin    
    
  set @existingInterviewConfirmationStatus = (select top 1 Status from CandidateStatus where CandidateId=@CandidateId and IRFId =@IRFId and InterviewId =@InterviewId order by id desc)    
    
 if(@Status=@existingInterviewConfirmationStatus)    
  begin    
   set @IsAdd=0    
  end    
 end    
    
     
    
    
 if(@IsAdd=1)    
 begin    
 INSERT INTO CandidateStatus (IRFId, CandidateId, AudioFileLink, Comments, [Date], [Status], [PerformedBy], InterviewId)    
    VALUES (@IRFId, @CandidateId, @AudioFileLink, @Comments, GETDATE(), @Status, @PerformedBy, @InterviewId)    
End    
    
End    
    
       
END    
    
  
GO


