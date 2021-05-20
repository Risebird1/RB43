
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
	   @interviewStatus int null
)  
RETURNS int  
AS  
BEGIN  
       -- Declare the return variable here  
       DECLARE @updatedPoints int  
       if(@meetingStatus = 1 or @meetingStatus=3)
			begin
			 set @updatedPoints = @perSuccessRate/4
			end
	   Else if(@meetingStatus=5)
			begin
			 set @updatedPoints = 0
			end
	   Else if(@meetingStatus=4 and @interviewStatus=3)
			Begin
				if(@videoDuration>=35)
					Begin
						set @updatedPoints=(35*@perSuccessRate)/60
					End
				else
					Begin
						set @updatedPoints=(@videoDuration*@perSuccessRate)/60
					End
			End
		Else if(@meetingStatus=4)
			Begin
				if(@videoDuration>=60)
					begin
						set @updatedPoints = @perSuccessRate
					end
				Else
					BEGIN
						set @updatedPoints = (@perSuccessRate * @videoDuration/60)
					END
			End 
       -- Return the result of the function  
       RETURN @updatedPoints  
END 

Go

