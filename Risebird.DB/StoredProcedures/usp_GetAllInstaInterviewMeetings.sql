IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllInstaInterviewMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllInstaInterviewMeetings
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO    
            
CREATE PROCEDURE [dbo].[usp_GetAllInstaInterviewMeetings]                
@startDate datetime,            
@endDate datetime,    
@EmailId varchar(100)=null    
AS     
Begin                      
          
if(@EmailId is not null and ((select idProfile from UserList where EmailId=@EmailId)=2))          
 Begin        
    Select * from InstaInterviewMeetings 
  where RequestedDate >= @startDate AND RequestedDate < @endDate and RequestedBy=@EmailId     
 End          
else          
 Begin          
    Select * from InstaInterviewMeetings 
  where RequestedDate >= @startDate AND RequestedDate < @endDate
 End          
              
                     
End                 
              
     