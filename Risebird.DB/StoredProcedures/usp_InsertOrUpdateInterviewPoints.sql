  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertOrUpdateInterviewPoints'))
BEGIN
    DROP PROCEDURE usp_InsertOrUpdateInterviewPoints
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_InsertOrUpdateInterviewPoints      
@interviewId int      
As      
BEGIN      
Declare @MeetingStatus int      
Declare @InterviewStatus int      
Declare @VideoDuration int=0      
Declare @UserId int      
Declare @CandidateEmail varchar(200)      
Declare @MaxVideoDuration int      
Declare @IsProctor bit       
Declare @IsEmergency bit       
Declare @Type varchar(20)='Interview'      
Declare @Count int      
Declare @Irfid int      
Declare @AddPoint int         
Declare @Points float    
Declare @FixedInterviewDuration bit
Declare @FixedAmount int = null
Declare @Interviewdate datetime

Select @MeetingStatus=MeetingStatus from InterviewComments where InterviewId=@interviewId      
Select @InterviewStatus=InterviewStatus from InterviewComments where InterviewId=@interviewId      
Select @VideoDuration=VideoDuration from InterviewComments where InterviewId=@interviewId      
Select @UserId=ul.id From InterviewMeetings im join UserList ul on im.interviewer=ul.EmailId where im.id=@interviewId      
Select @CandidateEmail=CandidateEmail From InterviewMeetings where id=@interviewId      
Select @MaxVideoDuration=IRF.InterviewDuration from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId where im.id=@interviewId      
Select @IsProctor=ISNULL(ca.IsProctor,0) from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join Userlist ul on im.candidateEmail=ul.EmailId           
 join CandidatesMaster cm on ul.id=cm.UserId join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id  where im.id=@interviewId                           
select @IsEmergency =IsEmergency from InterviewMeetings where id =@interviewId       
Select @Irfid=IRF.id from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId where im.id=@interviewId                           
Select @Count=count(*) from InterviewPoints where Interviewid=@interviewId and userid=@Userid and (InterviewPoints.Type=@Type or InterviewPoints.Type='Redo')                            
Select @FixedInterviewDuration=IsFixedInterviewduration From IRF where id=@Irfid   
Select @Interviewdate=Interviewdate from InterviewMeetings where id=@interviewId                           

if(@FixedInterviewDuration = 1)
Begin
	Select @FixedAmount=FixedAmount From IRF where id=@Irfid
End
      
   if(@IsEmergency=1)                          
     Begin                          
      If EXISTS(select top 1 ti.id from TransEmergencyInterviewer ti join IRF on irf.id=ti.IRFId where ti.Userid=@Userid and ti.IRFId=@Irfid)                          
      begin                                  
       Select @AddPoint=PerSuccessRate from TransEmergencyInterviewer where Userid=@Userid and IRFId=@Irfid                                  
      End                      
   else If EXISTS(select top 1 ti.id from TransInterviewer ti join IRF on irf.id=ti.IRFId where ti.Userid=@Userid and ti.IRFId=@Irfid)                          
       Begin                                  
        Select @AddPoint=PerSuccessRate from TransInterviewer where Userid=@Userid and IRFId=@Irfid                                  
       End                     
     END                          
                          
     Else                          
     BEGIN                          
      If EXISTS(select top 1 ti.id from TransInterviewer ti join IRF on irf.id=ti.IRFId where ti.Userid=@Userid and ti.IRFId=@Irfid)                          
       Begin                                  
        Select @AddPoint=PerSuccessRate from TransInterviewer where Userid=@Userid and IRFId=@Irfid                                  
       End                          
      Else If EXISTS(select top 1 ti.id from TransEmergencyInterviewer ti join IRF on irf.id=ti.IRFId where ti.Userid=@Userid and ti.IRFId=@Irfid)                          
       begin                                  
        Select @AddPoint=PerSuccessRate from TransEmergencyInterviewer where Userid=@Userid and IRFId=@Irfid                                  
       End                          
     END      
 if(@meetingStatus=2)    
  BEGIN  
  set @AddPoint=0  
  END  
 --Start:Meeting Status 1 or 3       
 if(@MeetingStatus=1 or @MeetingStatus=3)      
 BEGIN      
	 if(@VideoDuration>3)      
	 begin      
	 set @AddPoint=Round(((ISNULL(@AddPoint,0)*3)/60),0)                          
	 end      
	 else      
	 begin      
	 set @AddPoint=Round(((ISNULL(@AddPoint,0)*ISNULL(@VideoDuration,0))/60),0)                          
	 end      
  END      
  --End:Meeting Status 1 or 3       
  --Start:Meeting Status 4 or 6 or 7 or 8      
  if(@MeetingStatus=4 or @MeetingStatus=6 or @MeetingStatus=7 or @MeetingStatus=8)      
 BEGIN 
 if(@FixedInterviewDuration = 1)
 Begin
	set @AddPoint = @FixedAmount
 End
 Else
 Begin
 if(@VideoDuration>5 and @Interviewdate>='2023-05-01')
 begin
 set @VideoDuration=@VideoDuration-5;
 end
	if(@InterviewStatus=1 or @InterviewStatus=3)      
	 Begin 
		Set @MaxVideoDuration=ISNULL(@MaxVideoDuration,0)/2
	 End      
	 if(@VideoDuration>@MaxVideoDuration)      
	 begin      
	 set @AddPoint=Round(((ISNULL(@AddPoint,0)*@MaxVideoDuration)/60),0)                          
	 end      
	 else      
	 begin      
	 set @AddPoint=Round(((ISNULL(@AddPoint,0)*ISNULL(@VideoDuration,0))/60),0)                          
	 end
 End
      
END      
  --End:Meeting Status 4 or 6 or 7 or 8      
 if(@meetingStatus=5 or @MeetingStatus=9)                               
  BEGIN                  
  set @CandidateEmail='Penalty '+@CandidateEmail                  
  Declare @JobCountry varchar(50)                  
  Select @JobCountry=IRF.JobCountry from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId where im.id=@interviewId                  
  if(@JobCountry='India' or @JobCountry=1)                  
  BEGIN                  
  set @AddPoint=-200                  
  END                  
  Else                  
  BEGIN                  
  set @AddPoint=-350                  
  END                  
  END          
    
  if(@IsProctor=1)          
  BEGIN          
 if(@meetingStatus=4)      
 BEGIN      
  -- set for Selected or Rejected status with time check      
  if(@VideoDuration<=90)      
  BEGIN      
  set @AddPoint=150        
  END      
  else      
  BEGIN      
  set @AddPoint=250        
  END      
 END      
 else if(@meetingStatus=1)      
 BEGIN      
 set @AddPoint=50      
 END      
 else if(@meetingStatus=3)      
 BEGIN      
  if(@VideoDuration<=30)      
  BEGIN      
  set @AddPoint=75        
  END      
  else if(@VideoDuration<=90 and @VideoDuration>30)      
  BEGIN      
  set @AddPoint=150        
  END      
  else       
  BEGIN      
  set @AddPoint=250        
  END      
   END      
  set @CandidateEmail=REPLACE(@CandidateEmail,'Penalty','')          
  END       
    if(@Count=0)                            
    Begin                          
    Insert into InterviewPoints values(@interviewId,@Userid,@AddPoint,@Type,@CandidateEmail,'Pending',GetDate(),0,0)                             
    END                            
   Else                            
    BEGIN                            
    update InterviewPoints set Points = @AddPoint, [Date]=getdate(),Comment=@CandidateEmail,[Type]='Interview' where Interviewid = @interviewId and userid=@Userid and ([Type]='Interview' or [Type]='Redo') and Status='Pending'                             
    END             
      
END
GO  
  