/****** Object:  StoredProcedure [dbo].[usp_UpdateMeeting]    Script Date: 07-07-2020 11.46.28 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateMeeting'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateMeeting
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_UpdateMeeting]    
@interviewId int,
@IRFId varchar(200),
@interviewDate varchar(200),
@interviewSlot varchar(100),
@interviewDuration int,
@candidateEmail varchar(200),
@hrRiseBirdAssinged varchar(200),
@resumeLink varchar(MAX),
@zoomLink varchar(MAX),
@ClientHrContact varchar(200),
@comfirmationStatus varchar(100),
@interviewer varchar(200),
@preferredLocation varchar(200),
@expectedPerHourRate varchar(200),
@joiningTime varchar(200),
@staffingAgency varchar(200),
@candidateName varchar(200),
@currentEmployerName varchar(200),
@MobileNumber nvarchar(200),
@HRCoordinatorEmailId varchar(100),
@QCInterviewer varchar(200)

AS
Begin

	IF EXISTS (SELECT 1   FROM   InterviewMeetings  WHERE  id=@interviewId)
		Begin
			UPDATE  [dbo].[InterviewMeetings]
			SET         [IRFId] = @IRFId
					   ,[interviewDate] = @interviewDate
					   ,[interviewSlot] = @interviewSlot
					   ,[interviewDuration] = @interviewDuration
					   ,[candidateEmail] = @candidateEmail
					   ,[candidateName] = @candidateName
					   ,[ClientHrContact] = @ClientHrContact
					   ,[resumeLink] = @resumeLink
					   ,[confirmationStatus] = @comfirmationStatus
					   ,[zoomLink] = @zoomLink
					   ,[risebirdHR] = @hrRiseBirdAssinged
					   ,[PreferredLocation] = @preferredLocation
					   ,[ExpectedPerHourRate] = @expectedPerHourRate
					   ,[joiningTime] = @joiningTime
					   ,[staffingAgency] = @staffingAgency	   
					   ,[sModifiedBy] ='System'
					   ,[dtModifiedOn] = GETUTCDATE()
					   ,[currentEmployerName] = @currentEmployerName
					   ,[MobileNumber]= @MobileNumber
					   ,[interviewer]= @interviewer
					   ,[HRCoordinatorEmailId] =@HRCoordinatorEmailId
					   ,[QCInterviewer]=@QCInterviewer
			WHERE id = @interviewId        
    
		End
END






GO


