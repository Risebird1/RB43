/****** Object:  StoredProcedure [dbo].[usp_AddinterviewView]    Script Date: 4/18/2020 10:52:28 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddinterviewView'))
BEGIN
    DROP PROCEDURE dbo.usp_AddinterviewView
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure [dbo].[usp_AddinterviewView]
@Interviewid int,
@EmailId varchar(100)
AS  
BEGIN  
DECLARE @ViewCount int    
DECLARE @Id int  
DECLARE @Count int  
Set @Count=0  
select @Id=id from UserList where Emailid=@EmailId  
Select @ViewCount=TotalView from InterviewComments where InterviewId=@Interviewid    
Update InterviewComments set TotalView=((IsNull(@ViewCount,0))+1) where InterviewId=@Interviewid    
  
if(@Id=null)
begin
Select @Count=Count from VideoCountDetails where Videoid=@Interviewid and Emailid=@EmailId   
if(@Count=0)  
begin  
Insert into VideoCountDetails values(@Interviewid,0,1,GETDATE(),@EmailId)  
End  
else  
begin  
Update VideoCountDetails set Count=(@Count+1) where Videoid=@Interviewid and Emailid=@EmailId  
end  
end
else
begin
Select @Count=Count from VideoCountDetails where Videoid=@Interviewid and Userid=@Id
if(@Count=0)  
begin  
Insert into VideoCountDetails values(@Interviewid,@Id,1,GETDATE(),'')  
End  
else  
begin  
Update VideoCountDetails set Count=(@Count+1) where Videoid=@Interviewid and Userid=@Id  
end  
END
End
