/****** Object:  StoredProcedure [dbo].[usp_GetAccounts]    Script Date: 24-03-2020 8.20.45 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InterviewAddPoints'))
BEGIN
    DROP PROCEDURE dbo.usp_InterviewAddPoints
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
    
    
      
Create Procedure [dbo].[usp_InterviewAddPoints]             
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
 Declare @IsEmergency bit          
 select @IsEmergency =IsEmergency from InterviewMeetings where id =@interviewId        
 Select @Irfid=IRF.id from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId where im.id=@interviewId         
          
              
if(@Type='Interview')                
 BEGIN            
 Select @Userid=id from UserList where EmailId=(select interviewer from InterviewMeetings where id=@interviewId)           
 Select @Count=count(*) from InterviewPoints where Interviewid=@interviewId and userid=@Userid and InterviewPoints.Type=@Type           
  if(@Count=0)          
    Begin        
        
        
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
        
            
            
    --Select @Count=Count(*) from TransInterviewer ti join IRF on irf.id=ti.IRFId join InterviewMeetings im on irf.IRFId=im.IRFId where ti.Userid=@Userid and ti.IRFId=@Irfid and im.IsEmergency=0              
             
    --if(@Count>0)                
    -- Begin                
    -- Select @AddPoint=PerSuccessRate from TransInterviewer where Userid=@Userid and IRFId=@Irfid                
    -- End                
    --Else                
    -- begin                
    -- Select @AddPoint=PerSuccessRate from TransEmergencyInterviewer where Userid=@Userid and IRFId=@Irfid                
    -- End         
    set @AddPoint=Round((ISNULL(@AddPoint,0)*ISNULL(@Points,0)/100),0)        
	if(@Points=0)             
  BEGIN
  set @Candidate='Penalty '+@Candidate
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
    Select @Candidate=candidateEmail from InterviewMeetings where id=@interviewId           
    Insert into InterviewPoints values(@interviewId,@Userid,@AddPoint,@Type,@Candidate,'Pending',GetDate(),0,0)           
    END          
   Else          
    BEGIN          
        
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
        
            
    --Select @Count=Count(*) from TransInterviewer ti join IRF on irf.id=ti.IRFId join InterviewMeetings im on irf.IRFId=im.IRFId where ti.Userid=@Userid and ti.IRFId=@Irfid and im.IsEmergency=0              
    --if(@Count>0)                
    -- Begin                
    -- Select @AddPoint=PerSuccessRate from TransInterviewer where Userid=@Userid and IRFId=@Irfid                
    -- End                
    --Else                
    -- begin                
    -- Select @AddPoint=PerSuccessRate from TransEmergencyInterviewer where Userid=@Userid and IRFId=@Irfid                
    -- End         
             
        
    set @AddPoint= Round((ISNULL(@AddPoint,0)*ISNULL(@Points,0)/100), 0)          
    update InterviewPoints set Points = @AddPoint, [Date]=getdate() where Interviewid = @interviewId and userid=@Userid and [Type]='Interview'           
    END          
          
 END                
else if(@Type='Quality Controller')        
 Begin        
  Select @Userid=id from UserList where EmailId=(select QCInterviewer from InterviewMeetings where id=@interviewId)           
  Select @Count=count(*) from InterviewPoints where Interviewid=@interviewId and userid=@Userid and InterviewPoints.Type=@Type           
  if(@Count=0)          
   Begin          
    Select @AddPoint=PerSuccessRate from TransQualityController where Userid=@Userid and IRFId=@Irfid           
    set @AddPoint= Round((ISNULL(@AddPoint,0)*ISNULL(@Points,0)/100),0)               
     Select @Candidate=candidateEmail from InterviewMeetings where id=@interviewId                
    Insert into InterviewPoints values(@interviewId,@Userid,@AddPoint,@Type,@Candidate,'Pending',GetDate(),0,0)            
   END                
   else      
   Begin      
      if Not EXISTS(Select 1 from QCFeedbackReport where interviewid=@interviewId and QCStatus=1)      
   Begin      
   Delete from InterviewPoints where Interviewid=@interviewId and userid=@Userid and InterviewPoints.Type=@Type           
   END      
   END      
 END                
END         
  
GO


