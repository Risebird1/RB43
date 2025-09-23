Create Procedure usp_UpdateCandidateCancelOrCold 
@CandidateEmail Varchar(200),  
@IRFId varchar(15),  
@JobId varchar(20),  
@CurrentDate datetime,  
@RequestedBy varchar(200),  
@Type varchar(50)  
AS    
Begin  
DECLARE @TempCandidates TABLE  
(  
    Interviewid INT,  
    candidateemailId VARCHAR(200),  
 candidatestatus varchar(50)  
);  
Declare @Message varchar(50)  
Declare @RoleId int,@Userid int  
Set @Userid=(Select id From UserList where EmailId=@RequestedBy and isactive=1)  
Set @RoleId=(Select cu.RoleId From UserList ul join ClientUser cu on cu.UserId=ul.id where ul.EmailId=@RequestedBy)  
if(@Type='Candidate')  
begin  
 Declare @Interviewid int =0,@IRFIntid int,@AccessCount int=0,@CandidateId int=0,@MeetingStatus int  
 Declare @FKCandidateId int,@SkillChannelId varchar(15)  
 Set @CandidateId=(Select top 1 cm.id from CandidatesMaster cm join UserList ul on ul.id=cm.UserId where ul.EmailId=@CandidateEmail)  
  
 if(@CandidateId=0)  
 begin  
  Insert into @TempCandidates(Interviewid,candidateemailId,candidatestatus) values(0,@CandidateEmail,'Candidate Not Found')  
 end  
 if(@JobId!='')  
 begin  
  Set @SkillChannelId=(Select IRF.IRFId From Project pr join ProjectIRFMapping pir on pir.ProjectId=pr.id join Candidate ca on ca.IRFId=pir.IRFId and ca.CandidateMasterId=@CandidateId  
        join IRF on IRF.id=ca.IRFId where pr.ProjectUniqueid=@JobId)  
 end  
 if(@IRFId!='')  
 begin  
  Set @SkillChannelId=@IRFId  
 end  
 if(@SkillChannelId!='')  
 begin  
  Set @IRFIntid=(Select id From IRF where IRFId=@SkillChannelId)  
  if(@RoleId!=9)  
  begin  
   Set @AccessCount =(Select Count(id) From TransClientUser where ClientUserId=@Userid and IRFId=@IRFIntid and isactive=1)  
  end  
  else if(@Roleid=9)  
  begin  
   Set @AccessCount=1  
  end  
  if(@AccessCount>0)  
  begin  
   Select @Interviewid=id from InterviewMeetings where candidateEmail=@CandidateEmail and IRFId=@SkillChannelId and interviewDate>@CurrentDate and confirmationStatus=1 order by Interviewdate desc  
   if(@Interviewid>0)  
   begin  
    update [dbo].[InterviewMeetings] set confirmationStatus=2,CancellationReason='Cancel By Client',Cancellationsource=@RequestedBy where id = @Interviewid  
    INSERT INTO CandidateStatus (IRFId, CandidateId,  Comments, [Date], [Status], [PerformedBy],[InterviewId])          
    VALUES (@IRFIntid, @CandidateId,'Candidate Cancelled', @CurrentDate, 18, @RequestedBy,@Interviewid)     
    Insert into @TempCandidates(Interviewid,candidateemailId,candidatestatus) values(@Interviewid,@CandidateEmail,'Cancel the interview')  
   end  
   else  
   begin  
    Set @MeetingStatus=(Select top(1) ic.MeetingStatus from InterviewMeetings im join Interviewcomments ic on im.id=ic.InterviewId where candidateEmail=@CandidateEmail and IRFId=@SkillChannelId and ISNULL(im.RedoInterview,0)=0 and confirmationStatus=1 order by Interviewdate desc)  
    Set @Interviewid=(Select top(1) im.id from InterviewMeetings im where candidateEmail=@CandidateEmail and IRFId=@SkillChannelId and ISNULL(im.RedoInterview,0)=0 and confirmationStatus=1 order by Interviewdate desc)  
    if(@MeetingStatus=4 or @MeetingStatus=6 or @MeetingStatus=7 or @MeetingStatus=8)  
    begin  
     Insert into @TempCandidates(Interviewid,candidateemailId,candidatestatus) values(@Interviewid,@CandidateEmail,'Already Completed')  
    end  
    else  
    begin  
     Set @FKCandidateId=(Select id From Candidate where CandidateMasterId=@CandidateId and IRFId=@IRFIntid)  
     IF EXISTS(select 1 from ColdCandidateInfos where FKCandidateId=@FKCandidateId)  
     Begin  
      update ColdCandidateInfos set IsColdCandidate=1,ColdMarkedBY=@RequestedBy,ColdMarkedDate=@CurrentDate,ColdInfo='Drop By Client' where FKCandidateId=@FKCandidateId  
     End  
     Else  
     Begin  
      Insert Into ColdCandidateInfos(IsColdCandidate,ColdMarkedBY,ColdMarkedDate,FKCandidateId,ColdInfo) values(1,@RequestedBy,@CurrentDate,@FKCandidateId,'Drop By Client')  
     End  
     --update [dbo].[InterviewMeetings] set confirmationStatus=2,CancellationReason='Cold Candidate: Drop By Client',Cancellationsource=@RequestedBy where id = @Interviewid  
     Exec usp_AddCandidateStatusWebHookData @CandidateEmail, @SkillChannelId, 'coldcandidate'   
     Insert into @TempCandidates(Interviewid,candidateemailId,candidatestatus) values(0,@CandidateEmail,'Mark cold Candidate')  
    end  
   end  
  end  
  else  
  begin  
   Insert into @TempCandidates(Interviewid,candidateemailId,candidatestatus) values(0,@CandidateEmail,'You are not able to access this data')  
  end  
 end  
 else  
 begin  
  Insert into @TempCandidates(Interviewid,candidateemailId,candidatestatus) values(0,@CandidateEmail,'Candidate Not Found')  
 end  
end  
else if(@Type='AllData')  
begin  
    Declare @IRFIdTable Table  
 (  
  id int  
 )  
 Declare @InterviewTable Table  
 (  
  id int  
 )  
 Declare @CandidateColdTable Table  
 (  
  id int  
 )  
 Declare @ColdCandidateId int,@ColdCandidateEmailId varchar(200),@ColdCandidateIRFId varchar(15)  
 if(@JobId!='')  
 begin  
  if(@RoleId=9)  
  begin  
   Insert into @IRFIdTable Select pir.IRFId as id From Project pr join ProjectIRFMapping pir on pir.ProjectId=pr.id    
   where ProjectUniqueid=@JobId  
  end  
  else  
  begin  
   Insert into @IRFIdTable Select pir.IRFId as id From Project pr join ProjectIRFMapping pir on pir.ProjectId=pr.id    
   join TransClientUser tcu on tcu.IRFId=pir.IRFId  
   where ProjectUniqueid=@JobId and tcu.ClientUserid=@Userid and tcu.Isactive=1  
  end  
 end  
 if(@IRFId!='')  
 begin  
  if(@RoleId=9)  
  begin  
   Insert into @IRFIdTable Select id from IRF where IRFId=@IRFId  
  end  
  else  
  begin  
   Insert into @IRFIdTable Select IRFId as Id from TransClientUser where IRFId=@IRFId and ClientUserid=@Userid and Isactive=1  
  end  
 end  
--Start: Future Interviews  
 Insert Into @InterviewTable Select im.id from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join @IRFIdTable ir on ir.id=IRF.id  
 where interviewDate>@CurrentDate and confirmationStatus=1 order by Interviewdate desc  
 --Cancel the interview and Update status  
 update [dbo].[InterviewMeetings] set confirmationStatus=2,CancellationReason='Cancel By Client',Cancellationsource=@RequestedBy where id in(Select id from @InterviewTable)  
  
    INSERT INTO CandidateStatus (IRFId, CandidateId,  Comments, [Date], [Status], [PerformedBy],[InterviewId])          
 (Select IRF.id as IRFId,cm.id as CandidateId,'Candidate Cancelled' as Comments, @CurrentDate as [Date], 18 as [Status], @RequestedBy as PerformedBy,im.id as InterviewId From InterviewMeetings im join @InterviewTable it on it.id=im.id join IRF on IRF.IRFId=im.IRFId  
  join UserList ul on ul.Emailid=im.CandidateEmail join CandidatesMaster cm on cm.Userid=ul.id)  
  
 Insert into @TempCandidates(Interviewid,candidateemailId,candidatestatus)   
 (Select im.id as Interviewid,im.CandidateEmail as candidateemailId,'Cancel the interview' as candidatestatus From InterviewMeetings im join @InterviewTable it on it.id=im.id)  
--End: Future Interviews  
  
--Start : Past Interviews  
 delete from @InterviewTable  
 Insert Into @InterviewTable Select im.id from InterviewMeetings im join Interviewcomments ic on im.id=ic.InterviewId   
 join IRF on IRF.IRFid=im.IRFId join @IRFIdTable ir on ir.id=IRF.id   
 join UserList ul on ul.EmailId=im.candidateEmail  
 outer Apply dbo.fn_CandidateLatestStatusForIrfStatsForExcel(ul.id, IRF.id, @CurrentDate) La                  
 where ISNULL(im.RedoInterview,0)=0 and confirmationStatus=1 and (ic.MeetingStatus=1 or ic.MeetingStatus=2 or ic.MeetingStatus=3 or ic.MeetingStatus=5 or ic.MeetingStatus=9) and (La.LatestStatus not like '%SELECTED%' and La.LatestStatus not like '%REJECTED%' and La.LatestStatus not like '%COLDCANDIDATE%')  
  
  
 MERGE ColdCandidateInfos AS target  
 USING (  
  SELECT ca.id as FKCandidateId,IRF.Id as IRFIntid  
  FROM @InterviewTable t join InterviewMeetings im on im.id=t.id  
  join IRF on IRF.IRFid=im.IRFId  
  JOIN UserList ul on ul.Emailid = im.CandidateEmail  
  join CandidatesMaster cm on cm.Userid=ul.id  
  join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id  
 ) AS source  
 ON target.FKCandidateId = source.FKCandidateId   
 WHEN MATCHED THEN  
  UPDATE SET target.IsColdCandidate=1,  
        target.ColdMarkedBY=@RequestedBy,  
     target.ColdMarkedDate=@CurrentDate,  
     target.FKCandidateId=source.FKCandidateId,  
     target.ColdInfo ='Drop By Client'  
 WHEN NOT MATCHED BY TARGET THEN  
  Insert (IsColdCandidate,ColdMarkedBY,ColdMarkedDate,FKCandidateId,ColdInfo)   
  values(1,@RequestedBy,@CurrentDate,source.FKCandidateId,'Drop By Client')  
 OUTPUT inserted.FKCandidateId into @CandidateColdTable;      
--End : Past Interviews  
  
--Start: Not Scheduled  
 delete from @InterviewTable  
 Insert into @InterviewTable Select ca.id From Candidate ca join CandidatesMaster cm on cm.id=ca.CandidateMasterId  
 join @IRFIdTable ir on ir.id=ca.IRFId  
 join UserList ul on ul.id=cm.UserId  
 join IRF on IRF.Id=ca.IRFId  
 left join InterviewMeetings im on im.candidateEmail=ul.EmailId and im.IRFId=IRF.IRFId  
 where im.id is null  
  
 MERGE ColdCandidateInfos AS target  
 USING @InterviewTable  AS source  
 ON target.FKCandidateId = source.id   
 WHEN MATCHED THEN  
  UPDATE SET target.IsColdCandidate=1,  
        target.ColdMarkedBY=@RequestedBy,  
     target.ColdMarkedDate=@CurrentDate,  
     target.FKCandidateId=source.id,  
     target.ColdInfo ='Drop By Client'  
 WHEN NOT MATCHED BY TARGET THEN  
  Insert (IsColdCandidate,ColdMarkedBY,ColdMarkedDate,FKCandidateId,ColdInfo)   
  values(1,@RequestedBy,@CurrentDate,source.id,'Drop By Client')  
 OUTPUT inserted.FKCandidateId into @CandidateColdTable;  
--END: Not Scheduled  
  
--Start :Set Cold Candidate Data  
 WHILE EXISTS (SELECT 1 FROM @CandidateColdTable)  
 BEGIN  
  -- Pick one row (TOP 1)  
  SELECT TOP 1 @ColdCandidateId=id  
  FROM @CandidateColdTable  
  ORDER BY id;    
  -- Call your stored procedure  
  Set @ColdCandidateIRFId=(Select IRF.IRFId from Candidate ca join IRF on IRF.id=ca.IRFId where ca.id=@ColdCandidateId)  
  Set @ColdCandidateEmailId=(Select ul.Emailid from Candidate ca join CandidatesMaster cm on cm.id=ca.CandidateMasterId join UserList ul on ul.id=cm.Userid where ca.id=@ColdCandidateId)  
  Exec usp_AddCandidateStatusWebHookData @ColdCandidateEmailId, @ColdCandidateIRFId, 'coldcandidate'   
  Insert into @TempCandidates(Interviewid,candidateemailId,candidatestatus) values(0,@ColdCandidateEmailId,'Mark cold Candidate')  
  -- Remove the processed row so loop can move on  
  DELETE TOP (1)  
  FROM @CandidateColdTable  
  WHERE id = @ColdCandidateId;  
 END  
--End :Set Cold Candidate Data  
end  
Select * From @TempCandidates  
End