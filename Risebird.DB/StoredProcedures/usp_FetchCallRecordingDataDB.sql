IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchCallRecordingDataDB'))
BEGIN
    DROP PROCEDURE [dbo].[usp_FetchCallRecordingDataDB ]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO  
Create Procedure usp_FetchCallRecordingDataDB    
@UserProfile Varchar(15),      
@Emailid varchar(150)      
AS      
BEGIN      
if(@UserProfile = 'Admin')      
BEGIN    

select crd.id, crd.InterviewId, crd.IRFId, crd.[Caller], crd.CallRecipient, crd.DateCreated,
crd.Duration, crd.Price, crd.PriceUnit, crd.CallSid, crd.StartTime, crd.RecipientProfileType, crd.RecordingUri, crd.RecordingSid
from CallRecordingData crd

END 

END
GO

