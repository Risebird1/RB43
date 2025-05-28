IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateRedMarkeredInterviewerstatus'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateRedMarkeredInterviewerstatus
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_UpdateRedMarkeredInterviewerstatus        
@interviewid int,      
@IRFid int,      
@status int,      
@Type varchar(30),      
@Comment varchar(300),      
@CreatedBy varchar(200),      
@source varchar(30)       
AS        
BEGIN        
if(@source='TotalInterview')   
begin
	Update InterviewerProfile set RedMarkerStatus=@status where Userid=@interviewid
end
else
begin
	Declare @InterviewerProfileId int      
	Declare @RowCount int=0      
	Select @InterviewerProfileId=id From InterviewerProfile where UserId=@interviewid      
	Select @RowCount=Count(id) From TransInterviewerRedMarker where IRFId=@IRFid and InterviewerId=@InterviewerProfileId      
	if(@RowCount>0)      
	begin      
	--Update The row      
		 if(@source='RedMarker')      
		 BEGIN      
		  Update TransInterviewerRedMarker Set RedMarkerStatus=@status where IRFId=@IRFid and InterviewerId=@InterviewerProfileId      
		 END      
		 else      
		 BEGIN      
		  Update TransInterviewerRedMarker Set NegativeAIVerdictStatus=@status where IRFId=@IRFid and InterviewerId=@InterviewerProfileId      
		 END      
	end      
	else      
	begin      
	--Insert the row      
		 if(@source='RedMarker')      
		 BEGIN      
		  Insert Into TransInterviewerRedMarker (IRFId,InterviewerId,RedMarker,RedMarkerStatus) values (@IRFid,@InterviewerProfileId,0,@status)      
		 END      
		 else      
		 BEGIN      
		  Insert Into TransInterviewerRedMarker (IRFId,InterviewerId,RedMarker,NegativeAIVerdictStatus) values (@IRFid,@InterviewerProfileId,0,@status)      
		 END      
	end      
end
--Insert into Redmarker Comments With History        
Insert into RedMarkerStatusDetails(InterviewerUserid,IRFId,Comment,Type,CreatedOn,CreatedBy,Status)         
values(@interviewid,@IRFid,@Comment,@Type,DateAdd(Minute,330,GETDATE()),@CreatedBy,@status)        
END 
Go