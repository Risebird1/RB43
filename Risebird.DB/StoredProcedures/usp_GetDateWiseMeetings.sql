/****** Object:  StoredProcedure [dbo].[usp_GetDateWiseMeetings]    Script Date: 23-07-2020 5.16.16 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetDateWiseMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDateWiseMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
  
CREATE PROCEDURE [dbo].[usp_GetDateWiseMeetings]    
@dateValue datetime,
@Emailid varchar(150)
AS    
Begin   
Declare @Role varchar(150)
Select @Role=pr.Name From UserList ul join Profile pr on pr.id=ul.idprofile where Emailid=@Emailid
if(@Role='SeniorHR')
BEGIN
select im.interviewDate, irf.IRFId as IRFID, (irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill, ic.InterviewStatus from InterviewMeetings im join irf on
 irf.IRFId=im.IRFId  
join SeniorHr sh on sh.Clientid=irf.ClientId
left join UserList ulsh on ulsh.id=sh.Userid
left join InterviewComments ic on ic.InterviewId = im.id  
where cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 and ulsh.Emailid= @Emailid
END
ELSE
BEGIN
select im.interviewDate, irf.IRFId as IRFID, (irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill, ic.InterviewStatus from InterviewMeetings im join irf on
 irf.IRFId=im.IRFId  
left join InterviewComments ic on ic.InterviewId = im.id  
where cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1  
  END
End  
       


GO


