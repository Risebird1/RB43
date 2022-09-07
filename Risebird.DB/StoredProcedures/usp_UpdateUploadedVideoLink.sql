/****** Object:  StoredProcedure [dbo].[usp_GetAccounts]    Script Date: 24-03-2020 8.20.45 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateUploadedVideoLink'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateUploadedVideoLink
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_UpdateUploadedVideoLink]         
@interviewId int,        
@videoLink varchar(1000),        
@videoDuration int        
AS          
Begin         
        
update InterviewComments set VideoLink=@videoLink, VideoDuration = @videoDuration where InterviewId=@interviewId        
        
declare @meetingStatus int
set @meetingStatus=(select MeetingStatus from InterviewComments where InterviewId=@interviewId)        
 
if(@meetingStatus is not null and @meetingStatus!=2)        
Begin        
 Exec usp_InsertOrUpdateInterviewPoints @interviewId   
End        
End        
GO


