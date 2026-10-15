IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateRedMarkeredInterviewerstatus'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateRedMarkeredInterviewerstatus
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Returns one row: RowsChanged = rows moved (0 = unknown interviewer, nothing changed and no
-- history row written), so the page shows an error instead of a false "Moved Successfully".
-- RedMarkerStatusDetails.CreatedOn is IST: DATEADD(MINUTE, 330, GETUTCDATE()), as the other writers.
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
Declare @RowsChanged int = 0
if(@source='TotalInterview')   
begin
	Update InterviewerProfile set RedMarkerStatus=@status where Userid=@interviewid
	Set @RowsChanged = @@ROWCOUNT
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
		  Set @RowsChanged = @@ROWCOUNT
		 END      
		 else      
		 BEGIN      
		  Update TransInterviewerRedMarker Set NegativeAIVerdictStatus=@status where IRFId=@IRFid and InterviewerId=@InterviewerProfileId      
		  Set @RowsChanged = @@ROWCOUNT
		 END      
	end      
	else if(@InterviewerProfileId IS NOT NULL)
	begin      
	--Insert the row      
		 if(@source='RedMarker')      
		 BEGIN      
		  Insert Into TransInterviewerRedMarker (IRFId,InterviewerId,RedMarker,RedMarkerStatus) values (@IRFid,@InterviewerProfileId,0,@status)      
		  Set @RowsChanged = @@ROWCOUNT
		 END      
		 else      
		 BEGIN      
		  Insert Into TransInterviewerRedMarker (IRFId,InterviewerId,RedMarker,NegativeAIVerdictStatus) values (@IRFid,@InterviewerProfileId,0,@status)      
		  Set @RowsChanged = @@ROWCOUNT
		 END      
	end      
end
--Insert into Redmarker Comments With History        
IF @RowsChanged > 0
Insert into RedMarkerStatusDetails(InterviewerUserid,IRFId,Comment,Type,CreatedOn,CreatedBy,Status)         
values(@interviewid,@IRFid,@Comment,@Type,DATEADD(MINUTE, 330, GETUTCDATE()),@CreatedBy,@status)        
SELECT @RowsChanged AS RowsChanged
END 
Go