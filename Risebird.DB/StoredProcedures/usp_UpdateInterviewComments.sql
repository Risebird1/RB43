/****** Object:  StoredProcedure [dbo].[usp_UpdateInterviewComments]    Script Date: 4/13/2020 12:41:03 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateInterviewComments'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewComments
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



CREATE PROCEDURE [dbo].[usp_UpdateInterviewComments](
		@interviewId int, 
		@interviewStatus int = null,
			@interviewerComment varchar(500) = null,   
             @fraudCheckStatus int = null,
			  @hrComment varchar(500) = null,
			  @ClientInterviewStatus int = null,
			   @qcStatus int = null,
			    @qcComment varchar(500) = null,   
             @trainingStatus int = null,
			  @trainerComment varchar(500) = null,
			   @audiolink varchar(max) = null,
			    @videolink varchar(max) = null, 
				@MeetingStatus int = null, 
				@OfferStatus int = null, 
				@PreparationStatus int = null, 
				@JoiningStatus int =  null,
				@ThumbnailImage varchar(Max))  
AS  
Begin Try  
  BEGIN TRAN UpdateUser   
       
       IF EXISTS( SELECT 1 FROM InterviewMeetings where id = @InterviewId)  
     BEGIN  
   IF EXISTS(SELECT 1 FROM InterviewComments where interviewId= @InterviewId)  
     BEGIN  
     UPDATE [InterviewComments] SET interviewStatus = ISNULL(@InterviewStatus,interviewStatus) ,  
          interviewerComment =  ISNULL(@interviewerComment,interviewerComment),   
          FraudCheckStatus =  ISNULL(@FraudCheckStatus,FraudCheckStatus),  
          HRComment =  ISNULL(@HRComment,HRComment),  
          QCStatus = ISNULL(@QCStatus,QCStatus),  
          QCComment = ISNULL(@QCComment,QCComment),  
          TrainingStatus = ISNULL(@TrainingStatus,TrainingStatus),  
          TrainerComment = ISNULL(@TrainerComment,TrainerComment),  
          MeetingStatus = ISNULL(@MeetingStatus,MeetingStatus),  
          OfferStatus = ISNULL(@OfferStatus,OfferStatus),  
          PreparationStatus = ISNULL(@PreparationStatus,PreparationStatus),  
          JoiningStatus = ISNULL(@JoiningStatus,JoiningStatus) ,  
		   ClientInterviewStatus = ISNULL(@ClientInterviewStatus,ClientInterviewStatus), 
          AudioLink = ISNULL(@audiolink,AudioLink) ,  
          VideoLink = ISNULL(@videolink, VideoLink) ,
		  ThumbnailImage=ISNULL(@ThumbnailImage,ThumbnailImage)
      WHERE        interviewId = @InterviewId  
  
                      
     END  
   ELSE  
     BEGIN  
    INSERT INTO [InterviewComments]  
                      (interviewId,  
          interviewStatus,  
          interviewerComment,   
          FraudCheckStatus,  
          HRComment,  
          QCStatus,  
           QCComment,  
          TrainingStatus,  
          MeetingStatus,  
          OfferStatus,  
          PreparationStatus,  
          JoiningStatus, 
		  ClientInterviewStatus, 
          TrainerComment,
		  AudioLink,VideoLink,ThumbnailImage)   
    VALUES  
         (@interviewId,  
          @InterviewStatus,  
          @interviewerComment,  
          @FraudCheckStatus,  
          @HRComment,  
          @QCStatus,  
          @QCComment,  
          @TrainingStatus,  
          @MeetingStatus,  
          @OfferStatus,  
          @PreparationStatus,  
          @JoiningStatus,
		  @ClientInterviewStatus,  
          @TrainerComment,@audiolink,@videolink,@ThumbnailImage)  

	INSERT INTO 
	InterviewReport 
		(IRFId, 
		InterviewId, 
		CandidateName
		 )
	values
		(
			(select ir.id from IRF ir join InterviewMeetings im on ir.irfid = im.irfid where im.id = @interviewId),
			@interviewId,
			(select im.CandidateName from InterviewMeetings im where im.id = @interviewId)  
		)
        
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
  
  
  
  
  
  




