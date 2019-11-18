/****** Object:  StoredProcedure [dbo].[usp_UpdateEmergencyFlag]    Script Date: 18-11-2019 1.28.02 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateEmergencyFlag'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateEmergencyFlag
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_UpdateEmergencyFlag]
@id int,
@EmailId varchar(200)
AS
Begin

 
update InterviewMeetings set [IsEmergency] = 1 where id=@id
Insert into UnassignedMeetings values(@id, @EmailId)

	
End

GO



