/****** Object:  StoredProcedure [dbo].[usp_OpenEmergencyInterviews]    Script Date: 18-11-2019 6.52.24 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.[usp_OpenEmergencyInterviews'))
BEGIN
    DROP PROCEDURE dbo.[usp_OpenEmergencyInterviews]
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO





CREATE PROCEDURE [dbo].[usp_OpenEmergencyInterviews]       
   @emailID varchar(200)       
AS      
BEGIN        
select im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ulc.EmailId as candidateEmail, im.ClientHrContact,        
   im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,im.InterviewBiometryScore,        
   im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy,
   case 
	 when ulc.LastName is null then ulc.FirstName
	 else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))
   end as candidateName,        
 im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.JoiningBiometryScore, 
 irf.JDLink, irf.JDTiltle from InterviewMeetings im     
 inner join IRF irf on im.IRFId=irf.IRFId       
 inner join [dbo].[TransEmergencyInterviewer] tirf on IRF.id =tirf.IRFId      
 inner join [dbo].[Interviewer] inte on tirf.InterviewerId = inte.id      
 inner join [dbo].[UserList] ul on inte.UserId = ul.id  
 left join UserList ulc on ulc.EmailId = im.candidateEmail        
 join CandidatesMaster cm on ulc.id = cm.UserId
 join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id        

 left join UnassignedMeetings um on im.id=um.InterviewId     
 where interviewer is null and confirmationStatus = 1 and IsEmergency=1 and ul.EmailId = @emailID and um.EmailId !=  @emailID     
        
END   
GO


