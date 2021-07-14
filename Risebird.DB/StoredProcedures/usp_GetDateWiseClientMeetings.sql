/****** Object:  StoredProcedure [dbo].[usp_GetDateWiseClientMeetings]    Script Date: 06-08-2020 5.43.56 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetDateWiseClientMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDateWiseClientMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetDateWiseClientMeetings]  
@dateValue datetime,
@emailId varchar(100)
AS  
Begin   
  
declare @profileId int, @roleId int, @userId int;  
 set @profileId = (select id from [Profile] where [Name]='Client')  
 set @userId  = (select id from UserList where EmailId=@emailId and idProfile=@profileId)  
 set @roleId  = (select RoleId from ClientUser where UserId=@userId)  
   
  
if(@roleId=1)  
  
 Begin  
  select im.interviewDate, irf.IRFId as IRFID, (irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill, ic.InterviewStatus from InterviewMeetings im join irf 
  on irf.IRFId=im.IRFId  
  left join InterviewComments ic on ic.InterviewId = im.id  
  join UserList ulc on im.candidateEmail= ulc.EmailId      
  join CandidatesMaster cm on ulc.id= cm.UserId      
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId
  where cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and (im.ClientHrContact =@emailId  or ca.ClientHRAttemptContact=@emailId)
 End  
  
else if(@roleId=4)  
 Begin  
  select im.interviewDate, irf.IRFId as IRFID, (irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill, ic.InterviewStatus from InterviewMeetings im join irf 
  on irf.IRFId=im.IRFId  
  left join InterviewComments ic on ic.InterviewId = im.id  
  where cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and im.staffingAgency =@emailId  
 End  
  
else  
 Begin  
  select im.interviewDate, irf.IRFId as IRFID, (irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill, ic.InterviewStatus from InterviewMeetings im join irf 
  on irf.IRFId=im.IRFId  
  left join InterviewComments ic on ic.InterviewId = im.id  
  inner join TransClientUser tcu on tcu.IRFId = irf.id  
  where cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and tcu.ClientUserId=@userId  
 End  
  
End 

GO


