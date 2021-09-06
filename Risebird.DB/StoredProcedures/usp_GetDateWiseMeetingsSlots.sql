/****** Object:  StoredProcedure [dbo].[usp_GetDateWiseMeetingsSlots]    Script Date: 24-07-2020 9.15.24 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetDateWiseMeetingsSlots'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDateWiseMeetingsSlots
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
  
    
CREATE PROCEDURE [dbo].[usp_GetDateWiseMeetingsSlots]        
@dateValue datetime      
AS        
Begin     
    
    
DECLARE @tbl_MorningUnassigned TABLE (    
    IRFId varchar(200),    
    MorningUnassinged int     
);    
INSERT INTO @tbl_MorningUnassigned    
select IRFId, count(id) from InterviewMeetings where  confirmationStatus=1 and cast(interviewDate as date) = cast (@dateValue as date) and interviewSlot=1 and interviewer is null group by IRFId;    
    
DECLARE @tbl_EveningUnassigned TABLE (    
    IRFId varchar(200),    
    EveningUnassinged int     
);    
INSERT INTO @tbl_EveningUnassigned    
select IRFId, count(id) from InterviewMeetings where  confirmationStatus=1 and cast(interviewDate as date) = cast (@dateValue as date) and interviewSlot=2 and interviewer is null group by IRFId;    
    
    
    
    
 select distinct irf.IRFId, (irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill,    
 (select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=1 and cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and confirmationStatus=1) as Morning,    
 --(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=1 and cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and confirmationStatus=1 and im.interviewer is null) as MorningUnassinged,    
 (select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=2 and cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and confirmationStatus=1) as Evening,(ten.EveningUnassinged),(tmn.MorningUnassinged),    
 --(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and interviewSlot=2 and cast(interviewDate as date) = cast (@dateValue as date) and im.interviewSlot=2 and im.confirmationStatus=1 and confirmationStatus=1 and im.interviewer is null and interviewer=null) as EveningUnassinged,    
 (select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and confirmationStatus=1) as total,    
 (select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus=1  and confirmationStatus=1) as Proxy,    
 (select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus=2  and confirmationStatus=1) as Selected,    
 (select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus=3  and confirmationStatus=1) as Rejected,    
 (select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus=4  and confirmationStatus=1) as BorderLine,    
 (select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus is null and MeetingStatus=1) as NoShowCount,    
 (select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1) as ScheduledCount,    
 (select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and InterviewStatus is null and MeetingStatus=3  and confirmationStatus=1) as DroppedCount,    
 (select count(inm.id) from interviewmeetings inm left join InterviewComments inc on inc.InterviewId = inm.id where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and inm.InterviewDate<dateadd(minute,330,getdate()) and MeetingStatus=2 ) as Pending ,   
 (select Count(inm.id) from interviewmeetings inm join interviewComments inc on inc.InterviewId = inm.id  left join QCFeedbackReport iqc on inm.id=iqc.Interviewid  where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and confirmationStatus=1 and MeetingStatus=4 and iqc.id is null) as QCPending    
 from InterviewMeetings im join irf on irf.IRFId = im.IRFId    
 left join @tbl_EveningUnassigned ten on ten.IRFId = irf.IRFId    
 left join @tbl_MorningUnassigned tmn on tmn.IRFId = irf.IRFId    
 where cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1     
 group by IRF.IRFId, irf.JDTiltle, im.IRFId, im.confirmationStatus,im.interviewer, im.interviewSlot, ten.IRFId, ten.EveningUnassinged, tmn.IRFId, tmn.MorningUnassinged order by total desc    
    
End    
    
      
   
    
   
    
  
    
   

  

GO

