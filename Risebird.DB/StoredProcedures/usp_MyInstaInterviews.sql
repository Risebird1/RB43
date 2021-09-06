IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_MyInstaInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_MyInstaInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_MyInstaInterviews]             
@emailID varchar(200)                
AS              
BEGIN        
declare @emailIDParam varchar(200)    
set @emailIDParam=@emailID    
select distinct im.*,ir.*,im.id as id,RequiredSkill=STUFF((SELECT DISTINCT ', ' + CAST(SkillId AS VARCHAR(MAX)) FROM TransInstaSkills t2 WHERE t2.IRFId = tis.IRFId FOR XML PATH('')),1,1,'') from InstaInterviewMeetings im 
join InstaRequests ir on ir.id=im.RequestId 
left join TransInstaSkills tis on tis.IRFId=im.RequestId  
where interviewer =@emailIDParam and confirmationStatus = 1                                         
END         
GO