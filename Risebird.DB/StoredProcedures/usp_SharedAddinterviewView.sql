IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SharedAddinterviewView'))
BEGIN
    DROP PROCEDURE dbo.usp_SharedAddinterviewView
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Procedure [dbo].[usp_SharedAddinterviewView]    
@Interviewid int,    
@EmailId varchar(100)    
AS      
BEGIN      
DECLARE @ViewCount int        
DECLARE @Count int      
Set @Count=0      
Select @ViewCount=TotalView from SharedInterviewComments where Id=@Interviewid        
Update SharedInterviewComments set TotalView=((IsNull(@ViewCount,0))+1) where Id=@Interviewid        
      
    
Select @Count=Count from SharedVideoCountDetails where Videoid=@Interviewid and Emailid=@EmailId       
if(@Count=0)      
begin      
Insert into SharedVideoCountDetails values(@Interviewid,0,1,GETDATE(),@EmailId,'','')      
End      
else      
begin      
Update SharedVideoCountDetails set Count=(@Count+1) where Videoid=@Interviewid and Emailid=@EmailId      
end      
End    
  
  