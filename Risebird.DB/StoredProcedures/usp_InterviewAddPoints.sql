IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InterviewAddPoints'))
BEGIN
    DROP PROCEDURE dbo.usp_InterviewAddPoints
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


    
      
CREATE Procedure usp_InterviewAddPoints     
@interviewId int,       
@Type varchar(30),    
@Points float    
AS    
BEGIN 
Declare @Userid int    
Declare @AddPoint int    
Declare @Candidate varchar(200)    
Declare @Count int=0    
Declare @Irfid int
Select @Userid=id from UserList where EmailId=(select interviewer from InterviewMeetings where id=@interviewId)    
Select @Irfid=IRF.id from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId where im.id=@interviewId    
Select @Count=count(*) from InterviewPoints where Interviewid=@interviewId and userid=@Userid and InterviewPoints.Type=@Type  

if(@Count=0)    
	Begin    
		if(@Type='Interview')    
			BEGIN    
				Select @Count=Count(*) from TransInterviewer where Userid=@Userid and IRFId=@Irfid    
			if(@Count>0)    
				Begin    
					Select @AddPoint=PerSuccessRate from TransInterviewer where Userid=@Userid and IRFId=@Irfid    
				End    
			Else    
				begin    
					Select @AddPoint=PerSuccessRate from TransEmergencyInterviewer where Userid=@Userid and IRFId=@Irfid    
				End    
			END    
		else if(@Type='Quality Controller')    
			Begin    
				Select @AddPoint=PerSuccessRate from TransQualityController where Userid=@Userid and IRFId=@Irfid    
			END    
		set @AddPoint=(ISNULL(@AddPoint,0)*ISNULL(@Points,0)/100)   
	
		Select @Candidate=candidateEmail from InterviewMeetings where id=@interviewId    
		Insert into InterviewPoints values(@interviewId,@Userid,@AddPoint,@Type,@Candidate,'Pending',GetDate(),0)    
	END    

ELSE
	BEGIN
		if(@Type='Interview')    
			BEGIN    
				Select @Count=Count(*) from TransInterviewer where Userid=@Userid and IRFId=@Irfid    
			if(@Count>0)    
				Begin    
					Select @AddPoint=PerSuccessRate from TransInterviewer where Userid=@Userid and IRFId=@Irfid    
				End    
			Else    
				begin    
					Select @AddPoint=PerSuccessRate from TransEmergencyInterviewer where Userid=@Userid and IRFId=@Irfid    
				End    
			END
		set @AddPoint=(ISNULL(@AddPoint,0)*ISNULL(@Points,0)/100)

		update InterviewPoints set Points = @AddPoint, [Date]=getdate()
		where Interviewid = @interviewId and userid=@Userid and [Type]='Interview' 
	END
END    

GO

