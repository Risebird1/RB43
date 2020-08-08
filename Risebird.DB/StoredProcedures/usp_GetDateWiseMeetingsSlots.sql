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
  
select irf.IRFId, (irf.irfID + '('+ CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END +')' ) as IRFnSkill,  
(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and confirmationStatus=1 and interviewSlot=1 and cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1) as Morning,  
(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and confirmationStatus=1 and interviewSlot=2 and cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1) as Evening,  
(select count(interviewSlot) from InterviewMeetings where IRFId=im.IRFId and cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1) as total  
from InterviewMeetings im join irf on irf.IRFId = im.IRFId  
where cast(interviewDate as date) = cast (@dateValue as date) and im.confirmationStatus=1 group by IRF.IRFId, irf.JDTiltle, im.IRFId, im.confirmationStatus order by total desc  
  
End  
  

GO


