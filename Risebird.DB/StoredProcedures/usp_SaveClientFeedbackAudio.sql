/****** Object:  StoredProcedure [dbo].[usp_SaveFeedbackAudio]    Script Date: 05-06-2020 5.25.56 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveClientFeedbackAudio'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveClientFeedbackAudio
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



 
Create PROCEDURE [dbo].[usp_SaveClientFeedbackAudio]     
@InterviewId int,
@FeedbackAudioLink nvarchar(max),
@emailId varchar(50),
@timeNow datetime


AS      
BEGIN      
   declare @irfId int
   set @irfId = (select irf.id from IRF join InterviewMeetings im on im.IRFId = irf.IRFId where im.id = @InterviewId)
   if not exists(select 1 from ClientAudioFeedbackForReport where InterviewId = @InterviewId)
   begin

   insert into ClientAudioFeedbackForReport
			([InterviewId],
             [AudioUrl],
			 [CreatedBy],
			 [CreatedAt],
			 [IRFId])
		     values
		     (@InterviewId,		   
			  @FeedbackAudioLink,
			  @emailId,
			  @timeNow,
			  @irfId)
		   
   end
   else
   begin
   
   update ClientAudioFeedbackForReport set 
   AudioUrl = @FeedbackAudioLink,
   ModifiedAt = @timeNow,
   IRFId = @irfId
   where InterviewId=@InterviewId and CreatedBy = @emailId
   end
END   
GO


