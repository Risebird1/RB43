-- =============================================
-- SP Name:  dbo.usp_AddMeeting
-- Author: Prem     
-- Create date: 7th July 2019
-- Description: Add Meeting
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddMeeting'))
BEGIN
    DROP PROCEDURE dbo.usp_AddMeeting
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddMeeting]
@IRFName varchar(200),
@clientName varchar(200),
@JobRole varchar(200),
@interviewDate varchar(200),
@interviewDuration varchar(50),
@interviewSlot varchar(50),
@primaryskill varchar(500),
@candidateEmail varchar(200),
@ClientHrContact varchar(200),
@resumeLink varchar(MAX),
@zoomLink varchar(MAX),
@zoomDetails varchar(MAX),
@comfirmationStatus varchar(100),
@interviewer varchar(200),
@risebirdHR varchar(200)
AS
Begin
DECLARE 
  @IRFId int = (select  [id] from [dbo].[IRF] where Name = @IRFName)
INSERT INTO [dbo].[InterviewMeetings]
           ([IRFId]
           ,[clientName]
           ,[JobRole]
           ,[interviewDate]
		   ,[interviewSlot]
           ,[interviewDuration]
           ,[primaryskill]
           ,[candidateEmail]           
           ,[ClientHrContact]
           ,[resumeLink]
           ,[zoomLink]
           ,[zoomDetails]
           ,[comfirmationStatus]
           ,[interviewer]
           ,[risebirdHR]  
		   ,[dtCreatedOn]       
		   )
     VALUES
           (@IRFId,
@clientName,
@JobRole,
@interviewDate,
@interviewSlot,
@interviewDuration,
@primaryskill,
@candidateEmail,
@ClientHrContact,
@resumeLink,
@zoomLink,
@zoomDetails,
@comfirmationStatus,
@interviewer,
@risebirdHR,
getdate())


	
	
End



GO



