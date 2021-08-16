IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_MyInstaInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_MyInstaInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_MyInstaInterviews]           
@emailID varchar(200)              
AS            
BEGIN      
declare @emailIDParam varchar(200)  
set @emailIDParam=@emailID  
select * from InstaInterviewMeetings where interviewer =@emailIDParam and confirmationStatus = 1                    
END        
GO