
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[func_ComputeInterviewerPoints]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.func_ComputeInterviewerPoints
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
  
  
Create FUNCTION [dbo].[func_ComputeInterviewerPoints]    
(    
       @persuccessRate int,  
    @videoDuration int,  
    @meetingStatus int,  
    @interviewStatus int null,
	@jobCountry varchar(20)
)    
RETURNS int    
AS    
BEGIN    
       -- Declare the return variable here    
       DECLARE @updatedPoints int    
       if(@meetingStatus = 1 or @meetingStatus=3)  
   begin  
     if(@videoDuration=0)  
     Begin  
      set @updatedPoints=0  
     End  
    Else if(@videoDuration>=6)  
     Begin  
      set @updatedPoints=Round((6*@perSuccessRate)/60,0)  
     End  
    else  
     Begin  
      set @updatedPoints=Round((@videoDuration*@perSuccessRate)/60, 0)  
     End  
   end  
    Else if(@meetingStatus=5)  
   begin  
   if(@jobCountry='India' or @jobCountry='1')
   begin
   set @updatedPoints = -200  
   end
   else
   begin
   set @updatedPoints = -350 
   end
    
   end  
    Else if((@meetingStatus=4 or @meetingStatus=7)and (@interviewStatus=3 or @interviewStatus=1))  
   Begin  
    if(@videoDuration=0)  
     Begin  
      set @updatedPoints=0  
     End  
    Else if(@videoDuration>=30)  
     Begin  
      set @updatedPoints=Round((30*@perSuccessRate)/60,0)  
     End  
    else  
     Begin  
      set @updatedPoints=Round((@videoDuration*@perSuccessRate)/60, 0)  
     End  
   End  
  Else if((@meetingStatus=4 or @meetingStatus=6 or @meetingStatus=8) and (@interviewStatus=2 or @interviewStatus=4))  
   Begin  
    if(@videoDuration>=60)  
     begin  
      set @updatedPoints = @perSuccessRate  
     end  
    Else  
     BEGIN  
      set @updatedPoints = Round((@perSuccessRate * @videoDuration/60), 0)  
     END  
   End   
       -- Return the result of the function    
       RETURN @updatedPoints    
END   
  

Go

