/****** Object:  StoredProcedure [dbo].[usp_CheckCompletedInterviews]    Script Date: 25-09-2019 7.40.51 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CheckCompletedInterviews'))
BEGIN
    DROP PROCEDURE usp_CheckCompletedInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

create PROCEDURE [dbo].[usp_CheckCompletedInterviews]     
@emailID varchar(200)
AS      
BEGIN   

select count(*) from InterviewMeetings   
 where confirmationStatus = 1  and candidateEmail=@emailID  
      
END   
GO




