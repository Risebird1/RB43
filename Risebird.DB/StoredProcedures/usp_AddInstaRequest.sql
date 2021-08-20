IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInstaRequest'))
BEGIN
    DROP PROCEDURE dbo.usp_AddInstaRequest
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_AddInstaRequest    
@InstaRequestName varchar(15),  
@AssessmentGroup int,  
@JDLink varchar(1000),  
@RequestedBy varchar(150)  
AS  
BEGIN  
IF NOT EXISTS (SELECT 1 FROM InstaRequests WHERE InstaRequestName = @InstaRequestName and AssessmentGroup=@AssessmentGroup)      
Begin      
 Insert into [InstaRequests](InstaRequestName,AssessmentGroup,JDLink,RequestedBy,CreatedOn)    
 values(@InstaRequestName,@AssessmentGroup,@JDLink,@RequestedBy,dateadd(minute,330,GetDate()))    
End  
Else  
BEGIN  
Update InstaRequests Set JDLink=ISNULL(@JDLink,JDLink) WHERE InstaRequestName = @InstaRequestName and AssessmentGroup=@AssessmentGroup  
END  
Select id from InstaRequests WHERE InstaRequestName = @InstaRequestName and AssessmentGroup=@AssessmentGroup  
END
GO