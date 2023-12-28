IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SavePoorQOSDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_SavePoorQOSDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SavePoorQOSDetails]    
@participantEmail varchar(100),     
@ZoomMeetingId varchar(200),  
@qosType varchar(50),  
@max_jitter varchar(15),
@min_jitter varchar(15),
@avg_jitter varchar(15),
@quality varchar(25),
@quality_score varchar(15),
@max_latency varchar(15),
@avg_loss varchar(15),
@max_loss varchar(15),
@user_name varchar(50),
@CreatedAt datetime
AS    
Begin  

Insert into PoorQOSZoom (ZoomMeetingId, participantEmail, qosType, max_jitter, min_jitter, avg_jitter, quality_score, max_latency, avg_loss, max_loss, CreatedAt, quality, [user_name])
values(@ZoomMeetingId, @participantEmail, @qosType, @max_jitter, @min_jitter, @avg_jitter, @quality_score, @max_latency, @avg_loss, @max_loss, @CreatedAt, @quality, @user_name)  


End
GO

