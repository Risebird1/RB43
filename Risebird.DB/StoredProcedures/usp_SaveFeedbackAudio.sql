/****** Object:  StoredProcedure [dbo].[usp_SaveFeedbackAudio]    Script Date: 05-06-2020 5.25.56 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveFeedbackAudio'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveFeedbackAudio
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



 
Create PROCEDURE [dbo].[usp_SaveFeedbackAudio]     
@InterviewId int,
@FeedbackAudioLink nvarchar(max)


AS      
BEGIN      
   
   if not exists(select 1 from InterviewComments where InterviewId = @InterviewId)
   begin

   insert into InterviewComments
			([InterviewId],
             [FeedbackAudioLink])
		     values
		     (@InterviewId,		   
			  @FeedbackAudioLink)
		   
   end
   else
   begin
   
   update InterviewComments set 
   FeedbackAudioLink=@FeedbackAudioLink
   where InterviewId=@InterviewId
   end
END   
GO


