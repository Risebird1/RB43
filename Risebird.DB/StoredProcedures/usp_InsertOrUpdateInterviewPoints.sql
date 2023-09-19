  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertOrUpdateInterviewPoints'))
BEGIN
    DROP PROCEDURE usp_InsertOrUpdateInterviewPoints
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
      
        
CREATE Procedure usp_InsertOrUpdateInterviewPoints              
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
Declare @IsNewSkillChannel bit        
Declare @PlanType int        
Declare @JobCountry varchar(50)          
Declare @RateForMin int=60        
Declare @Interviewdate datetime        
Declare @CompanyType int  
        
Select @JobCountry=IRF.JobCountry,@CompanyType=IRF.CompanyType from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId where im.id=@interviewId                          
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
Select @FixedInterviewDuration=IsFixedInterviewduration From IRF where id=@Irfid and Isnull(IsNewSkillChannel,0)=0        
Select @IsNewSkillChannel=Isnull(IsNewSkillChannel,0) From IRF where id=@Irfid         
Select @PlanType=Heuristicsscore From IRF where id=@Irfid         
Select @Interviewdate=Interviewdate from InterviewMeetings where id=@interviewId           
        
if(@IsNewSkillChannel=1)        
begin        
 --if(@PlanType=2)        
 --Begin        
 --Select @MaxVideoDuration=DurationSelectedMin From PlanTypeChargesMaster where PlanType='Standard' and Countrytype=@JobCountry and CompanyType=@CompanyType       
 --End        
 --else if(@PlanType=4)        
 --Begin        
 --Select @MaxVideoDuration=DurationSelectedMin From PlanTypeChargesMaster where PlanType='Pro' and Countrytype=@JobCountry  and CompanyType=@CompanyType           
 --End        
 --else if(@PlanType=5)        
 --Begin        
 --Select @MaxVideoDuration=DurationSelectedMin From PlanTypeChargesMaster where PlanType='Advanced' and Countrytype=@JobCountry  and CompanyType=@CompanyType           
 --End        
 Select @MaxVideoDuration=DurationSelectedMin From PlanTypeChargesMaster where PlanTypeEnumValue=@PlanType and Countrytype=@JobCountry and CompanyType=@CompanyType       
 set @RateForMin=@MaxVideoDuration        
end        
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
 --Start:Meeting Status 3               
 if(@MeetingStatus=3 or @MeetingStatus=1)              
 BEGIN              
  --if(@VideoDuration>3)              
  --begin              
  --set @AddPoint=Round(((ISNULL(@AddPoint,0)*3)/@RateForMin),0)                                  
  --end              
  --else              
  --begin              
  --set @AddPoint=Round(((ISNULL(@AddPoint,0)*ISNULL(@VideoDuration,0))/@RateForMin),0)                                  
  --end        
  set @AddPoint=0      
  END              
  --End:Meeting Status 3               
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
 if(@IsNewSkillChannel!=1 and (@InterviewStatus=1 or @InterviewStatus=3))              
  Begin         
  Set @MaxVideoDuration=ISNULL(@MaxVideoDuration,0)/2        
  End              
  if(@VideoDuration>@MaxVideoDuration)              
  begin              
  set @AddPoint=Round(((ISNULL(@AddPoint,0)*@MaxVideoDuration)/@RateForMin),0)                                  
  end              
  else              
  begin              
  set @AddPoint=Round(((ISNULL(@AddPoint,0)*ISNULL(@VideoDuration,0))/@RateForMin),0)                                  
  end        
 End        
              
END              
  --End:Meeting Status 4 or 6 or 7 or 8              
 if(@meetingStatus=5 or @MeetingStatus=9)                                       
  BEGIN                          
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
  END               
    if(@Count=0)                                    
    Begin                                  
    Insert into InterviewPoints(Interviewid,userid,Points,Type,Comment,Status,Date,InterviewerId,PaymentHistoryId,PenaltyAmount,QCPoints)
    values(@interviewId,@Userid,@AddPoint,@Type,@CandidateEmail,'Pending',GetDate(),0,0,0,0)                                     
    END                                    
   Else                                    
    BEGIN                                    
    update InterviewPoints set Points = @AddPoint, [Date]=getdate(),Comment=@CandidateEmail,[Type]='Interview' where Interviewid = @interviewId and userid=@Userid and ([Type]='Interview' or [Type]='Redo') and Status='Pending'                             
  
    END                     
END           
GO  
  