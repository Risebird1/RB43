IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInterviewRequest'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddInterviewRequest]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_AddInterviewRequest          
@id int,        
@JdTitle varchar(50),            
@PrimarySkill varchar(200),            
@JDLink varchar(1000),    
@JobLevel int,
@RequestedBy varchar(150),
@ExpectedInterviews int,
@Country varchar(50)
AS            
BEGIN            
IF NOT EXISTS (SELECT 1 FROM InterviewRequests WHERE id=@id)                
Begin      
if Not Exists(Select 1 from InterviewRequests WHERE JdTitle=@JdTitle and PrimarySkill=@PrimarySkill and RequestedBy=@RequestedBy)    
BEGIN    
 Insert into [InterviewRequests](JdTitle,JobLevel,PrimarySkill,JDLink,RequestedBy,CreatedOn,ExpectedInterviews,Country)              
 values(@JdTitle,@JobLevel,@PrimarySkill,@JDLink,@RequestedBy,dateadd(minute,330,GetDate()),@ExpectedInterviews,@Country)              
 Select id from InterviewRequests WHERE JdTitle=@JdTitle and PrimarySkill=@PrimarySkill
 END    
End            
Else            
BEGIN            
Update InterviewRequests Set JDLink=ISNULL(@JDLink,JDLink),JdTitle=ISNULL(@JdTitle,JdTitle),ExpectedInterviews=@ExpectedInterviews,Country=@Country WHERE id=@id           
select @id           
END            
      
END      
GO

          
            
            
              


        
      
    