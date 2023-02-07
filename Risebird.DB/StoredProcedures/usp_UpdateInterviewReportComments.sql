/****** Object:  StoredProcedure [dbo].[usp_UpdateInterviewComments]    Script Date: 4/13/2020 12:41:03 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateInterviewReportComments'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewReportComments
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_UpdateInterviewReportComments](  
  @interviewId int,   
  @interviewStatus int = null,  
  @interviewerComment varchar(MAX) = null,   
  @audiolink varchar(max) = null,  
  @videolink varchar(max) = null,   
  @MeetingStatus int = null,     
  @ThumbnailImage varchar(Max),  
  @FeedbackAudioLink nvarchar(max) =null,  
  @ThumbnailTimeSeconds int = null,  
  @VideoDuration int = null,
  @PerformedBy varchar(100),
  @PerformedAt datetime)  
AS    
Begin Try    
  BEGIN TRAN UpdateUser     
 Declare @NickName varchar(10), @totalTechSkillScore float

set @totalTechSkillScore = (select sum(WeightageActual) from TechSkillReport where InterviewId = @interviewId)
Select @NickName=ISNULL(ip.NickName,'') from InterviewMeetings im join UserList ul on ul.EmailId=im.interviewer join InterviewerProfile ip on ip.UserId=ul.id where im.id=@InterviewId        
 
 IF EXISTS( SELECT 1 FROM InterviewMeetings where id = @InterviewId)    
  BEGIN    
   IF EXISTS(SELECT 1 FROM InterviewComments where interviewId= @InterviewId)    
    BEGIN  
     
     UPDATE [InterviewComments] SET interviewStatus = @interviewStatus ,    
        interviewerComment =  ISNULL(@interviewerComment,interviewerComment),    
        MeetingStatus = @MeetingStatus,   
        AudioLink = ISNULL(@audiolink,AudioLink) ,    
        VideoLink = ISNULL(@videolink, VideoLink) ,  
        ThumbnailImage=ISNULL(@ThumbnailImage,ThumbnailImage),  
        FeedbackAudioLink = ISNULL(@FeedbackAudioLink, FeedbackAudioLink),  
     ThumbnailTimeSeconds = ISNULL(@ThumbnailTimeSeconds, ThumbnailTimeSeconds),  
     VideoDuration = ISNULL(@VideoDuration, VideoDuration),
	 InterviewerNickName=ISNULL(@NickName,InterviewerNickName),
	 ModifiedBy = @PerformedBy,
	 ModifiedOn = @PerformedAt,
	 TotalTechScore = @totalTechSkillScore
     WHERE interviewId = @InterviewId    
    
                        
    END    
   ELSE    
    BEGIN    
     INSERT INTO [InterviewComments]    
       (interviewId,    
        interviewStatus,    
        interviewerComment,    
        MeetingStatus,          
        AudioLink,  
        VideoLink,  
        ThumbnailImage,  
        FeedbackAudioLink,  
        ThumbnailTimeSeconds,  
        VideoDuration,
		InterviewerNickName,
		CreatedBy,
		CreatedOn,
		TotalTechScore)     
     VALUES    
       (@interviewId,    
        @InterviewStatus,    
        @interviewerComment,  
        @MeetingStatus,   
        @audiolink,  
        @videolink,  
        @ThumbnailImage,  
        @FeedbackAudioLink,  
        @ThumbnailTimeSeconds,  
        @VideoDuration,
		@NickName,
		@PerformedBy,
		@PerformedAt,
		@totalTechSkillScore)    
  
     IF NOT EXISTS(SELECT 1 FROM InterviewReport where InterviewId= @InterviewId)  
     BEGIN  
      INSERT INTO   
      InterviewReport   
       (IRFId,   
       InterviewId,   
       CandidateName,
	   CreatedBy,
	   CreatedOn)  
         
       values  
       (  
        (select ir.id from IRF ir join InterviewMeetings im on ir.irfid = im.irfid where im.id = @interviewId),  
        @interviewId,  
        (select im.CandidateName from InterviewMeetings im where im.id = @interviewId),
		@PerformedBy, @PerformedAt
       )  
  
     END  
  
       
          
    END    
  
    
  END  
       
 Commit TRAN UpdateUser;    
End Try     
Begin Catch    
 RAISERROR ('Error While updating Interview Comments', -- Message text.    
   16, -- Severity.    
   1 -- State.    
   );    
    
 Rollback;    
End Catch    
  

GO
  
  
  
  

